using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DockerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var nginxConf = "/etc/nginx/nginx.conf";

            //Start nginx
            var processStartInfo = new ProcessStartInfo($"nginx -c {nginxConf}");
            using (var process = Process.Start(processStartInfo))
            {
                
            }

            using(var watcher = new FileSystemWatcher())
            {
                watcher.Path = Path.GetDirectoryName(nginxConf);
                watcher.Filter = Path.GetFileName(nginxConf);

                watcher.Changed += Watcher_Changed;
            }

            Console.WriteLine("Press 'q' to quit the proxy.");
            while (Console.Read() != 'q') ;
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Reloading nginx.");
            using (var reload = Process.Start("nginx", "-s reload"))
            {
                reload.WaitForExit();
            }
        }
    }
}
