using System;

namespace dropdownloadcore
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = args[0];
            var path = args[1];
            var pat = args[2];
            var destination = args[3];
            
            // https://msasg.artifacts.visualstudio.com/DefaultCollection/_apis/drop/drops/Aether_master/7dd31c59986465bfa9af3bd883cb35ce132979a2/e90d7f94-265a-86c7-5958-66983fdcaa06
            Console.WriteLine($"url:{url}");
            // /Release/Amd64/app/aether/AetherBackend
            Console.WriteLine($"relateivepath:{url}");
            Console.WriteLine($"pat:{pat}");
            Console.WriteLine($"dest:{destination}");
            //should be evironment variable SYSTEM_ACCESSTOKEN
            var proxy = new VSTSDropProxy(url, path, pat);
            proxy.Materialize(destination).Wait();
        }
    }
}
