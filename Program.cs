using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace DropDownloadCore
{
    sealed class Program
    {
        private const string RelavePathEnvironmentVariable = "relativepath";
        private const string VSTSPatEnvironmentVariable = "vstspat";
        private const string DropDestinationEnvironmentVariable = "dropdestination";
        private const string DefaultDropDestination = "/drop";
        private const string DropUrlEnvironmentVariable = "dropurl";

     
        static void Main(string[] args)
        {
            var relativePath = System.Environment.GetEnvironmentVariable(RelavePathEnvironmentVariable) ?? "/";
            var pat = System.Environment.GetEnvironmentVariable(VSTSPatEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(pat) || pat.Equals("$(System.AccessToken)")) 
            {
               throw new ArgumentException("Invalid personal accestoken. Remember to set allow scripts to access oauth token in agent phase");
            }
                
            var destination = System.Environment.GetEnvironmentVariable(DropDestinationEnvironmentVariable)
                              ?? DefaultDropDestination;
            var url = System.Environment.GetEnvironmentVariable(DropUrlEnvironmentVariable)
                      ?? ExtractDropUrl(destination);

            // sample URL:
            // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
            Console.WriteLine($"url: {url}");
            // /Release/Amd64/app/aether/AetherBackend
            Console.WriteLine($"relative path: {relativePath}");
            Console.WriteLine($"destination: {destination}");
            var proxy = new VSTSDropProxy(url, relativePath, pat);
            var sw = Stopwatch.StartNew();
            proxy.Materialize(destination).Wait();
            sw.Stop();

            Console.WriteLine($"Finished in {sw.Elapsed}");
        }

        // agent based tasks automatically download artifacts from the build. 
        // when the build only produces a vsts drop that artifact is a single json
        // it resides in <builddefname>/<guid>/VSTSDrop.json
        private static string ExtractDropUrl(string workingDirectory)
        {
            //could take an envdir on what the build dir is for now though we just have one build
            string buildDirectory = string.Empty;
            string guidDirectory = string.Empty;

            try
            {
                buildDirectory = Directory.GetDirectories(workingDirectory).Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"The working directory, {workingDirectory}, is invalid");
                throw;
            }

            try
            {
                guidDirectory = Directory.GetDirectories(buildDirectory).Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"The build directory, {buildDirectory}, is invalid");
                throw;
            }

            var dropJSONFilename = Path.Combine(workingDirectory, buildDirectory,  guidDirectory, "VSTSDrop.json");

            // https://www.newtonsoft.com/json/help/html/DeserializeAnonymousType.htm
            var definition = new { VstsDropBuildArtifact = new {VstsDropUrl ="" } };
            var artifact = JsonConvert.DeserializeAnonymousType(File.ReadAllText(dropJSONFilename), definition);
            return artifact.VstsDropBuildArtifact.VstsDropUrl;            
        }
    }
}
