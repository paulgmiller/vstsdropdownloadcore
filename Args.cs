using System;
using CommandLine;

namespace DropDownloadCore
{
    public class Args
    {
        private const string RelavePathEnvironmentVariable = "relativepath";
        private const string VSTSPatEnvironmentVariable = "vstspat";
        private const string DropDestinationEnvironmentVariable = "dropdestination";
        private const string DefaultDropDestination = "/drop";
        private const string DropUrlEnvironmentVariable = "dropurl";

        [Option]
        public string VstsPat { get; set; } = Environment.GetEnvironmentVariable(VSTSPatEnvironmentVariable);

        [Option]
        public string DropDestination { get; set; } =
            System.Environment.GetEnvironmentVariable(DropDestinationEnvironmentVariable)
            ?? DefaultDropDestination;

        [Option]
        public string DropUrl { get; set; } = Environment.GetEnvironmentVariable(DropUrlEnvironmentVariable);

        [Option]
        public string RelativePath { get; set; } = Environment.GetEnvironmentVariable(RelavePathEnvironmentVariable) 
                                                   ?? "/";
        
        [Option]
        public int BlobTimeoutSeconds { get; set; } = int.Parse(Environment.GetEnvironmentVariable("BlobTimeoutSeconds") ?? "15");

        [Option]
        public string InstrumentationKey { get; set; } = "5af8641f-fe42-4661-b431-849b73b55e0c";

        public Args()
        {
        }

        public void ValidatePat()
        {
            if (string.IsNullOrWhiteSpace(this.VstsPat) || this.VstsPat.Equals("$(System.AccessToken)"))
            {
                throw new ArgumentException("Invalid personal accestoken. Remember to set allow scripts to access oauth token in agent phase");
            }
        }
    }
}
