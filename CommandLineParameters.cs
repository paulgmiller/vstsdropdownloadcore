using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace dropdownloadcore
{
    public class CommandLineParameters
    {
        [Verb("get", HelpText = "Download drop" )]
        public class DownloadOptions
        {
            [Option('u', "url", Required = true, HelpText = "Drop URL, for example https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06 ")]
            public string Url { get; set; }

            [Option('p', "path", Required = true, HelpText = "Realtive path in drop", Default = "/Release/Amd64/app/aether/AetherBackend")]
            public string RelativePath { get; set; }

            [Option("pat", Required = true, HelpText = "PAT")]
            public string Pat { get; set; }

            [Option('d', "destination", Required = true, HelpText = "Destination path")]
            public string DestinationPath { get; set; }
        }
    }
}
