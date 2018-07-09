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

        [Option('t', "VstsPat")]
        public string VstsPat { get; set; } = Environment.GetEnvironmentVariable(VSTSPatEnvironmentVariable);

        [Option('d', "DropDestination")]
        public string DropDestination { get; set; } =
            System.Environment.GetEnvironmentVariable(DropDestinationEnvironmentVariable)
            ?? DefaultDropDestination;

        [Option('u', "DropUrl")]
        public string DropUrl { get; set; } = Environment.GetEnvironmentVariable(DropUrlEnvironmentVariable);

        [Option('p', "RelativePath")]
        public string RelativePath { get; set; } = Environment.GetEnvironmentVariable(RelavePathEnvironmentVariable) 
                                                   ?? "/";

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
