using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using CommandLine;

namespace DropDownloadCore
{
    sealed class Program
    {
        static void Main(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<Args>(args)
                 .WithParsed<Args>(a => Run(a));
        }

        static void Run(Args a)
        {
            a.ValidatePat();
            var url = a.DropUrl ?? ExtractDropUrl(a.DropDestination);

            // sample URL:
            // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
            Console.WriteLine($"url: {url}");
            // /Release/Amd64/app/aether/AetherBackend
            Console.WriteLine($"relative path: {a.RelativePath}");
            Console.WriteLine($"destination: {a.DropDestination}");
            var proxy = new VSTSDropProxy(url, a.RelativePath, a.VstsPat);
            var sw = Stopwatch.StartNew();
            proxy.Materialize(a.DropDestination).Wait();
            sw.Stop();

            Console.WriteLine($"Finished in {sw.Elapsed}");
        }

        // agent based tasks automatically download artifacts from the build. 
        // when the build only produces a vsts drop that artifact is a single json
        // it resides in <builddefname>/<guid>/VSTSDrop.json
        private static string ExtractDropUrl(string workingDirectory)
        {
            //could take an envdir on what the build dir is for now though we just have one build
            string guidDirectory = string.Empty;
            string currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine(currDir);
            string[] childDirectories = Directory.GetDirectories(currDir);

            Console.WriteLine("Child directories: ");
            foreach (string dir in childDirectories)
            {
                Console.WriteLine(dir);
            }

            var parentPath = Path.GetFullPath("/");
            string[] siblingDirectories = Directory.GetDirectories(parentPath);
            Console.WriteLine("sibling directories: ");
            foreach (string dir in siblingDirectories)
            {
                Console.WriteLine(dir);
            }

            try
            {
                guidDirectory = Directory.GetDirectories(workingDirectory).Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"The build directory, {workingDirectory}, is invalid");
                throw;
            }

            var dropJSONFilename = Path.Combine(workingDirectory,  guidDirectory, "VSTSDrop.json");

            // https://www.newtonsoft.com/json/help/html/DeserializeAnonymousType.htm
            var definition = new { VstsDropBuildArtifact = new {VstsDropUrl ="" } };
            var artifact = JsonConvert.DeserializeAnonymousType(File.ReadAllText(dropJSONFilename), definition);
            return artifact.VstsDropBuildArtifact.VstsDropUrl;            
        }
    }
}
