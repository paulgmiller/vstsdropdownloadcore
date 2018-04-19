using System;
using System.Diagnostics;
namespace dropdownloadcore
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = System.Environment.GetEnvironmentVariable("vstsdropurl");
            var relativepath = System.Environment.GetEnvironmentVariable("relativepath");
            var pat = System.Environment.GetEnvironmentVariable("vstspat");
            var destination = "/drop";
            
            // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
            Console.WriteLine($"url:{url}");
            // /Release/Amd64/app/aether/AetherBackend
            Console.WriteLine($"relativepath:{relativepath}");
            Console.WriteLine($"pat:{pat}");
            Console.WriteLine($"dest:{destination}");
            var sw = Stopwatch.StartNew();
            //should be evironment variable SYSTEM_ACCESSTOKEN
            var proxy = new VSTSDropProxy(url, relativepath, pat);
            proxy.Materialize(destination).Wait();
            Console.WriteLine($"Finished in {sw.Elapsed} ");
        }
    }
}
