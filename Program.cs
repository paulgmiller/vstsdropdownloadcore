using CommandLine;
using Newtonsoft.Json;
using System;

namespace dropdownloadcore
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CommandLineParameters.DownloadOptions>(args) // list other CommandLineParameters.Blah as part of template
             .MapResult(
               (CommandLineParameters.DownloadOptions opts) => DownloadDrop(opts), // list other CommandLineParameters.Blah mappings here

               errs => 1);
        }

        private static int DownloadDrop(CommandLineParameters.DownloadOptions opts)
        {
            Console.WriteLine($"Request: {JsonConvert.SerializeObject(opts, Formatting.Indented)}");

            var proxy = new VSTSDropProxy(VSTSDropUri: opts.Url, path: opts.RelativePath, pat: opts.Pat);
            proxy.Materialize(opts.DestinationPath).Wait();
            return 0;
        }
    }
}
