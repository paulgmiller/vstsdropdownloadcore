using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Web;

using Polly;

namespace DropDownloadCore
{
    //use vsts drop rest api for manifest and blob and a PAT to grab urls to files and hackily materialize them.
    public class VSTSDropProxy 
    {
        // sigh these went public moths ago. Check if we can use non preview versions
        private const string ManifestAPIVersion = "2.0-preview";
        private const string BlobAPIVersion = "2.1-preview";

        private readonly IDropApi _dropApi = null;
        private readonly HttpClient _contentClient = new HttpClient();

        private readonly Uri _VSTSDropUri;
        private readonly string _relativeroot;
        
        private readonly ISet<VstsFile> _files;
        
        public VSTSDropProxy(string VSTSDropUri, string path, string pat)
        {
            
            _dropApi = new RestfulDropApi(pat);
              
            if (!Uri.TryCreate(VSTSDropUri, UriKind.Absolute, out _VSTSDropUri))
            {
                throw new ArgumentException($"VSTS drop URI invalid {VSTSDropUri}", nameof(VSTSDropUri));
            }
            
            if (path == null)
            {
                throw new ArgumentException($"VSTS drop URI must contain a ?root= querystring {_VSTSDropUri}", nameof(VSTSDropUri));
            }
            
            _relativeroot = path.Replace("\\", "/");
            if (!_relativeroot.StartsWith("/"))
                _relativeroot = "/" + _relativeroot;
            if (!_relativeroot.EndsWith("/"))
                _relativeroot = _relativeroot + "/";

            //move this to a lazy so we can actually be async?
            try
            {
                var manifesturi = Munge(_VSTSDropUri, ManifestAPIVersion);
                _files = _dropApi.GetVstsManifest(manifesturi, BlobAPIVersion, _relativeroot ).Result;
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
            var querystring = HttpUtility.ParseQueryString(vstsDropUri.Query);
            string manifestpath = vstsDropUri.AbsolutePath.Replace("_apis/drop/drops", "_apis/drop/manifests");
            var uriBuilder = new UriBuilder(vstsDropUri.Scheme, vstsDropUri.Host, -1, manifestpath);
            var queryParameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryParameters.Add(RestfulDropApi.APIVersionParam,apiVersion);
            uriBuilder.Query = queryParameters.ToString();
            
            return uriBuilder.Uri;
        }

        private async Task Download(string sasurl, string localpath)
        {
            await Policy
                .Handle<HttpRequestException>()
                .Or<IOException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(5, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (e,t) => 
                    {
                        Console.WriteLine($"Exception {e} on {sasurl} -> {localpath}");
                        File.Delete(localpath);
                    })
                .ExecuteAsync(async () =>
                {
                    using (var blob = await _contentClient.GetStreamAsync(sasurl).ConfigureAwait(false))
                    using (var fileStream = new FileStream(localpath, FileMode.CreateNew))
                    {
                        await blob.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                });
        }
        
         //So this only dedupes within a build.
         //other options for perf.
         // 1) only grab certain directories either with dockerfiles or as specified by build.yaml
         // 2) Prioritize large files or files with lots of copies.
         // 3) linux symlink instead of copy
         // 4) parallelize copy with buffer first attempt at that with _contentClient.GetBufferAsync failed. Also lots of memory.
         // 5) multistream copyasync
         // Tried configureawait(false) and copyaync to each file (though not aparallelized) with no effect.
        public async Task Materialize(string localDestiantion)
        {
            var uniqueblobs = _files.GroupBy(file => file.Blob.Id).ToList();
            Console.WriteLine($"Found {_files.Count} files, {uniqueblobs.Count} unique");
            
            var dockerdirs = new HashSet<string>();
            //precreate directories so we don't have to worry.
            //slow but we don't care cause we want to move off and have sangam use blobs directly
            foreach (var file in _files) //could do blobs.selectmany(vallues)
            {
                var replativepath = file.Path.Substring(_relativeroot.Length);
                var localpath = Path.Combine(localDestiantion,replativepath).Replace("\\","/");
                //also not efficient to check directory each time 
                
                Directory.CreateDirectory(Path.GetDirectoryName(localpath));
                var filename = Path.GetFileName(localpath);
                if (filename.StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase))
                {
                    dockerdirs.Add(Path.GetDirectoryName(replativepath));
                }
             }

            //Altenatively would be neat to hash as we iterate throgh first loop
            
            int downloaded = 0;
            var downloads = uniqueblobs.Select(async group => 
            {
                var f = group.First();
                var relativepath = f.Path.Substring(_relativeroot.Length);
                var localPath = Path.Combine(localDestiantion,relativepath).Replace("\\","/");
                await Download(f.Blob.Url, localPath);
                 
                // parallelize this too? worth it?
                foreach (var other in group.Skip(1))
                {
                    var otherrelativepath = other.Path.Substring(_relativeroot.Length);
                    var otherpath = Path.Combine(localDestiantion,otherrelativepath).Replace("\\","/");
                    File.Copy(localPath, otherpath);
                }
                if (++downloaded % 100 == 0)
                {
                    Console.WriteLine($"Downloaded {downloaded} files");
                }                
            });
            await Task.WhenAll(downloads);
        } 
      
        private string HashFiles(IEnumerable<VstsFile> files)
        {
            var hasher = new System.Security.Cryptography.SHA256Managed();
            hasher.Initialize();
            foreach( var f in files.OrderBy(f => f.Path))
            {
                var buffer = Encoding.UTF8.GetBytes(f.Blob.Id);
                hasher.TransformBlock(buffer, 0, buffer.Length, null, 0);
                buffer = Encoding.UTF8.GetBytes(f.Path);
                hasher.TransformBlock(buffer, 0, buffer.Length, null, 0);
            }
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            //can we make this shorter by not using hex? base64 has chars that don't work in docker tags
            return System.BitConverter.ToString(hasher.Hash).Replace("-","");
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
