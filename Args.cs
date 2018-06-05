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
