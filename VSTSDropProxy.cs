using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Polly;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;
using System.Threading;
using System.Runtime.InteropServices;

namespace DropDownloadCore
{
    //use vsts drop rest api for manifest and blob and a PAT to grab urls to files and hackily materialize them.
    public class VSTSDropProxy
    {
        // sigh these went public moths ago. Check if we can use non preview versions
        private const string ManifestAPIVersion = "2.0-preview";
        private const string BlobAPIVersion = "2.1-preview";
        private int processedFileCount = 0;
        private readonly IDropApi _dropApi = null;
        private readonly HttpClient _contentClient;
        private readonly int _retryCount;
        private readonly bool _useSoftLinks;
        private readonly string _cacheLocation;
        private readonly int _concurrentDownloads;
        private readonly bool _computeDockerHashes;
        private readonly Uri _VSTSDropUri;
        private readonly string _relativeroot;

        private readonly IList<VstsFile> _files;

        public VSTSDropProxy(string VSTSDropUri, string path, string pat, TimeSpan blobtimeout, int retryCount, bool useSoftLinks, string cacheLocation, int concurrentDownloads, bool computeDockerHashes)
        {
            _dropApi = new RestfulDropApi(pat);
            _contentClient = new HttpClient() { Timeout = blobtimeout };
            _retryCount = retryCount;
            _useSoftLinks = useSoftLinks;
            _cacheLocation = cacheLocation;
            _concurrentDownloads = concurrentDownloads;
            _computeDockerHashes = computeDockerHashes;

            if (!Uri.TryCreate(VSTSDropUri, UriKind.Absolute, out _VSTSDropUri))
            {
                throw new ArgumentException($"VSTS drop URI invalid {VSTSDropUri}", nameof(VSTSDropUri));
            }

            if (path == null)
            {
                throw new ArgumentException($"VSTS drop URI must contain a ?root= querystring {_VSTSDropUri}", nameof(VSTSDropUri));
            }

            _relativeroot = path.Replace('\\', Path.DirectorySeparatorChar);
            if (!_relativeroot.StartsWith("/"))
            {
                _relativeroot = $"/{_relativeroot}";
            }

            if (!_relativeroot.EndsWith("/"))
            {
                _relativeroot += "/";
            }

            //move this to a lazy so we can actually be async?
            try
            {
                var manifesturi = Munge(_VSTSDropUri, ManifestAPIVersion);
                _files = _dropApi.GetVstsManifest(manifesturi, BlobAPIVersion, _relativeroot).Result;
            }
            catch (Exception)
            {
                Console.WriteLine($"Not able to get build manifest please check your build '{VSTSDropUri}'");
                throw;
            }

            if (!_files.Any())
            {
                throw new ArgumentException("Encountered empty build drop check your build " + VSTSDropUri);
            }
            //https://1eswiki.com/wiki/CloudBuild_Duplicate_Binplace_Detection
        }

        /// <summary>
        /// Gets the manifest uri from the drop url
        /// </summary>
        /// <param name="vstsDropUri">The drop url.</param>
        /// <param name="apiVersion">API version to request.</param>
        /// <returns>The manifest uri.</returns>
        private static Uri Munge(Uri vstsDropUri, string apiVersion)
        {
            string manifestpath = vstsDropUri.AbsolutePath.Replace("_apis/drop/drops", "_apis/drop/manifests");
            var uriBuilder = new UriBuilder(vstsDropUri.Scheme, vstsDropUri.Host, -1, manifestpath);
            var queryParameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParameters.Add(RestfulDropApi.APIVersionParam, apiVersion);
            uriBuilder.Query = queryParameters.ToString();
            return uriBuilder.Uri;
        }

        private async Task Download(string sasurl, string localpath)
        {
            await Policy
                .Handle<HttpRequestException>()
                .Or<SocketException>()
                .Or<IOException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(_retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (e, t) =>
                    {
                        Console.WriteLine($"Exception {e} on {sasurl} -> {localpath}");
                        if (File.Exists(localpath))
                        {
                            File.Delete(localpath);
                        }
                    })
                .ExecuteAsync(async () =>
                {
                    // todo: timeout based on blob size
                    using (var blob = await _contentClient.GetStreamAsync(sasurl))
                    using (var fileStream = new FileStream(localpath, FileMode.CreateNew))
                    {
                        await blob.CopyToAsync(fileStream);
                    }
                });
        }

        // other options for perf.
        // 1) only grab certain directories either with dockerfiles or as specified by build.yaml
        // 2) Prioritize large files or files with lots of copies.
        // 3) parallelize copy with buffer first attempt at that with _contentClient.GetBufferAsync failed. Also lots of memory.
        // 4) multistream copyasync
        public async Task<Dictionary<string, double>> Materialize(string localDestination)
        {
            var uniqueblobs = _files.GroupBy(keySelector: file => file.Blob.Id, resultSelector: (key, file) => file).ToList();
            Console.WriteLine($"Found {_files.Count} files, {uniqueblobs.Count} unique");
            var metrics = new Dictionary<string, double>
            {
                ["files"] = _files.Count,
                ["uniqueblobs"] = uniqueblobs.Count
            };

            if (_computeDockerHashes)
            {
                ComputeDockerHashes(localDestination, metrics);
            }

            var dltimes = new ConcurrentBag<double>();
            var copytimes = new ConcurrentBag<double>();
            var throttler = new ActionBlock<IEnumerable<VstsFile>>(list => DownloadGrouping(list, localDestination, dltimes, copytimes), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _concurrentDownloads });

            foreach (var grouping in uniqueblobs)
            {
                throttler.Post(grouping);
            }

            throttler.Complete();
            await throttler.Completion;

            if (dltimes.Any())
            {
                metrics["AverageDownloadSecs"] = dltimes.Average();
                metrics["MaxDownloadSecs"] = dltimes.Max();
            }

            if (copytimes.Any())
            {
                metrics["AverageCopySecs"] = copytimes.Average();
                metrics["MaxCopySecs"] = copytimes.Max();
            }

            return metrics;
        }

        private void ComputeDockerHashes(string localDestination, IDictionary<string, double> metrics)
        {
            var dockerdirs = new List<string>();
            foreach (var file in _files) //could do blobs.selectmany(vallues)
            {
                var replativepath = file.Path.Substring(_relativeroot.Length);
                var localpath = Path.Combine(localDestination, replativepath).Replace('\\', Path.DirectorySeparatorChar);
                Directory.CreateDirectory(Path.GetDirectoryName(localpath));
                var filename = Path.GetFileName(localpath);
                if (filename.StartsWith("dockerfile", StringComparison.OrdinalIgnoreCase))
                {
                    dockerdirs.Add(Path.GetDirectoryName(file.Path));
                }
            }

            metrics["dockerfiles"] = dockerdirs.Count;

            //Altenatively would be neat to hash as we iterate throgh first loop
            foreach (var ddir in dockerdirs)
            {
                var dirfiles = _files.Where(f => f.Path.StartsWith(ddir)).ToList();
                var hash = HashFiles(dirfiles);
                Console.WriteLine($"{ddir} ({dirfiles.Count})-> {hash}");
                var relativepath = ddir.Substring(_relativeroot.Length);
                var localPath = Path.Combine(localDestination, relativepath).Replace('\\', Path.DirectorySeparatorChar);
                File.WriteAllText(Path.Combine(localPath, ".dirhash"), hash);
            }
        }

        private async Task DownloadGrouping(IEnumerable<VstsFile> group, string localDestination, ConcurrentBag<double> dltimes, ConcurrentBag<double> copytimes)
        {
            var blob = group.First().Blob;
            var blobPath = Path.Combine(localDestination, _cacheLocation, blob.Id).Replace('\\', Path.DirectorySeparatorChar);
            if (!File.Exists(blobPath))
            {
                var downloadTimer = Stopwatch.StartNew();
                EnsureDirectory(blobPath);
                await Download(blob.Url, blobPath);
                downloadTimer.Stop();
                dltimes.Add(downloadTimer.Elapsed.TotalSeconds);
            }

            var copyTimer = Stopwatch.StartNew();
            foreach (var other in group)
            {
                var otherrelativepath = other.Path.Substring(_relativeroot.Length);
                var otherpath = Path.Combine(localDestination, otherrelativepath).Replace('\\', Path.DirectorySeparatorChar);
                EnsureDirectory(otherpath);
                File.Delete(otherpath);
                CreateLink(blobPath, otherpath, softLink: _useSoftLinks);
                var count = Interlocked.Increment(ref this.processedFileCount);
                if (count % 1000 == 0)
                {
                    Console.WriteLine($"Processed {count} files...");
                }
            }

            copytimes.Add(copyTimer.Elapsed.TotalSeconds);
        }

        private void CreateLink(string localPath, string otherpath, bool softLink)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                using (var process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "/bin/ln",
                    },
                })
                {
                    if (softLink)
                    {
                        process.StartInfo.ArgumentList.Add("-s");
                    }

                    process.StartInfo.ArgumentList.Add(localPath);
                    process.StartInfo.ArgumentList.Add(otherpath);
                    process.Start();
                }
            }
            else
            {
                File.Copy(localPath, otherpath);
            }

        }

        private void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }

        private string HashFiles(IEnumerable<VstsFile> files)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var f in files.OrderBy(f => f.Path))
            {
                builder.Append(f.Blob.Id);
                builder.Append(f.Path);
            }
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hasher = new System.Security.Cryptography.SHA1Managed();
            var hash = hasher.ComputeHash(bytes);
            //really we want to encode as base36
            return System.BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    // Helper classes for parsing VSTS drop exe output lowercase to match json output
    public sealed class VstsFile
    {
        public string Path { get; set; }

        public VstsBlob Blob { get; set; }

        public override int GetHashCode() { return StringComparer.OrdinalIgnoreCase.GetHashCode(Path); }
    }

    public sealed class VstsBlob
    {
        public string Url;

        public string Id;
    }
}
