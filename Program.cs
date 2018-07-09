using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
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
                a.ValidatePat();
                var url = a.DropUrl ?? ExtractDropUrl(a.DropDestination);
                props["url"] = url;
                // sample URL:
                // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
                Console.WriteLine($"url: {url}");
                // /Release/Amd64/app/aether/AetherBackend
                Console.WriteLine($"relative path: {a.RelativePath}");
                Console.WriteLine($"destination: {a.DropDestination}");
                var proxy = new VSTSDropProxy(url, a.RelativePath, a.VstsPat);
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

        private static bool IsVSTSBuild()
        {
            Console.WriteLine($"system host type: {Environment.GetEnvironmentVariable("SYSTEM_HOSTTYPE")}");
            return Environment.GetEnvironmentVariable("SYSTEM_HOSTTYPE") == "build";
        }

        private static string GetBuildFolder(string workingDirectory)
        {
            if (IsVSTSBuild())
            {
                return workingDirectory;
            }

            try
            {
                return Directory.GetDirectories(workingDirectory).Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"The working directory, {workingDirectory}, is invalid");
                throw;
            }
        }

        // agent based tasks automatically download artifacts from the build. 
        // when the build only produces a vsts drop that artifact is a single json
        // it resides in <builddefname>/<guid>/VSTSDrop.json
        private static string ExtractDropUrl(string workingDirectory)
        {
            string buildDirectory = GetBuildFolder(workingDirectory);
            string guidDirectory = string.Empty;

            try
            {
                // guidDirectory = Directory.GetDirectories(buildDirectory).Single();
                guidDirectory = Directory.GetDirectories(buildDirectory).Where(directory => directory.Remove(0,directory.LastIndexOf('/') + 1) != "drop").Single();
            }
            catch (Exception)
            {
                Console.WriteLine($"The build directory, {buildDirectory}, is invalid");
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
