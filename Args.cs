using System;
using CommandLine;

namespace DropDownloadCore
{
    public class Args
    {
        private const string RelativePathEnvironmentVariable = "relativepath";
        private const string VSTSPatEnvironmentVariable = "vstspat";
        private const string DropDestinationEnvironmentVariable = "dropdestination";
        private const string CacheLocationEnvironmentVariable = "cachelocation";
        private const string DefaultDropDestination = "/drop";
        private const string DefaultCacheLocation = "/drop/.dropcache";
        private const string DropUrlEnvironmentVariable = "dropurl";

        [Option('t', "VstsPat")]
        public string VstsPat { get; set; } = Environment.GetEnvironmentVariable(VSTSPatEnvironmentVariable);

        [Option('d', "DropDestination")]
        public string DropDestination { get; set; } =
            System.Environment.GetEnvironmentVariable(DropDestinationEnvironmentVariable)
            ?? DefaultDropDestination;

        [Option('c', "CacheLocation")]
        public string CacheLocation { get; set; } =
            System.Environment.GetEnvironmentVariable(CacheLocationEnvironmentVariable)
            ?? DefaultCacheLocation;

        [Option('u', "DropUrl")]
        public string DropUrl { get; set; } = Environment.GetEnvironmentVariable(DropUrlEnvironmentVariable);

        [Option('p', "RelativePath")]
        public string RelativePath { get; set; } = Environment.GetEnvironmentVariable(RelativePathEnvironmentVariable) ?? "/";

        [Option]
        public int BlobTimeoutSeconds { get; set; } = int.Parse(Environment.GetEnvironmentVariable("BlobTimeoutSeconds") ?? "15");

        [Option('r', "RetryCount")]
        public int RetryCount { get; set; } = int.Parse(Environment.GetEnvironmentVariable("RetryCount") ?? "10");

        [Option('n', "ConcurrentDownloads")]
        public int ConcurrentDownloads { get; set; } = int.Parse(Environment.GetEnvironmentVariable("ConcurrentDownloads") ?? "50");

        [Option('s', "SoftLinks")]
        public bool SoftLinks { get; set; } = bool.Parse(Environment.GetEnvironmentVariable("SoftLinks") ?? "false");

        [Option('i', "InstrumentationKey")]
        public string InstrumentationKey { get; set; } = "5af8641f-fe42-4661-b431-849b73b55e0c";

        [Option('h', "ComputeDockerHashes")]
        public bool ComputeDockerHashes { get; set; } = bool.Parse(Environment.GetEnvironmentVariable("ComputeDockerHashes") ?? "false");

        public Args()
        {
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.VstsPat) || this.VstsPat.Equals("$(System.AccessToken)"))
            {
                throw new ArgumentException($"Invalid personal acces token: '{this.VstsPat}'");
            }

            if (this.BlobTimeoutSeconds < 0 || this.BlobTimeoutSeconds > 3600)
            {
                throw new ArgumentException("blob timeout needs to be postive and less than an hour");
            }

            if (this.RetryCount < 0 || this.RetryCount > 20)
            {
                throw new ArgumentException("retry count must be within [0, 20]");
            }

            if (this.ConcurrentDownloads < 0 || this.ConcurrentDownloads > 1000)
            {
                throw new ArgumentException("concurrent downloads must be within [0, 1000]");
            }
        }
    }
}
