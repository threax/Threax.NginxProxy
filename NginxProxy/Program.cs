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
            Console.WriteLine("Starting Threax.NginxProxy");

            var nginxConf = "/data/config/nginx.conf";

            Console.WriteLine($"Looking for config {nginxConf}");
            while(!File.Exists(nginxConf))
            {
                Console.WriteLine($"File {nginxConf} does not exist. Will try again in 1 sec.");
                Thread.Sleep(1000);
            }

            //Start nginx
            Console.WriteLine($"Starting Nginx");
            var processStartInfo = new ProcessStartInfo("nginx", $"-c {nginxConf}");
            using (var process = Process.Start(processStartInfo))
            {
                
            }

            using(var watcher = new FileSystemWatcher())
            {
                watcher.Path = Path.GetDirectoryName(nginxConf);
                watcher.Filter = Path.GetFileName(nginxConf);

                watcher.Changed += Watcher_Changed;
                watcher.Created += Watcher_Changed;

                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Press 'q' to quit the proxy.");
                while (Console.Read() != 'q') ;
            }
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
