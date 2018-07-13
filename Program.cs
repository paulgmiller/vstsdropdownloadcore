using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using CommandLine;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

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
            var telemetry = new TelemetryClient(new TelemetryConfiguration(a.InstrumentationKey));
            //https://docs.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient.trackevent?view=azure-dotnet
            var props  = new Dictionary<string,string>();
            var metrics = new Dictionary<string,double>();
            var sw = Stopwatch.StartNew();
            try
            {
                a.Validate();
                var url = a.DropUrl ?? ExtractDropUrl(a.DropDestination);
                props["url"] = url;
                // sample URL:
                // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
                Console.WriteLine($"url: {url}");
                // /Release/Amd64/app/aether/AetherBackend
                Console.WriteLine($"relative path: {a.RelativePath}");
                Console.WriteLine($"destination: {a.DropDestination}");
                var proxy = new VSTSDropProxy(url, a.RelativePath, a.VstsPat, TimeSpan.FromSeconds(a.BlobTimeoutSeconds));
                metrics = proxy.Materialize(a.DropDestination).Result;
                Console.WriteLine($"Finished in {sw.Elapsed}");
                props["success"] = "True";
                
            } 
            catch (Exception e)
            {
                props["success"]  = "False";
                props["exception"] = e.ToString();
                throw;
            }
            finally
            {
                 metrics["Elapsed"] = sw.Elapsed.TotalSeconds;
                 telemetry.TrackEvent("dropdownloader", props, metrics);
                 telemetry.Flush();
            }
        }

        // agent based tasks automatically download artifacts from the build. 
        // when the build only produces a vsts drop that artifact is a single json
        // it resides in <builddefname>/<guid>/VSTSDrop.json
        private static string ExtractDropUrl(string workingDirectory)
        {
            //could take an envdir on what the build dir is for now though we just have one build
            string buildDirectory = string.Empty;
            string guidDirectory = string.Empty;
            
            string dropJSONFilename;
            try
            {
                var dropjsonfiles = Directory.GetFiles(workingDirectory, "VSTSDrop.json", SearchOption.AllDirectories);
                if (dropjsonfiles.Length == 0)
                {
                    throw new ArgumentException("Didn't find any vstsdrop.json");
                }
                if (dropjsonfiles.Length > 1)
                {
                    var allpaths = string.Join(";",dropjsonfiles);
                    throw new ArgumentException("Found multiple vstsdrop.json: {allpaths}");
                }
                dropJSONFilename = dropjsonfiles.Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"Trouble finding VSTSDrop.json in {workingDirectory}");
                throw;
            }

            // https://www.newtonsoft.com/json/help/html/DeserializeAnonymousType.htm
            var definition = new { VstsDropBuildArtifact = new {VstsDropUrl ="" } };
            var artifact = JsonConvert.DeserializeAnonymousType(File.ReadAllText(dropJSONFilename), definition);
            return artifact.VstsDropBuildArtifact.VstsDropUrl;            
        }
    }
}
