using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
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
        static async Task Main(string[] args)
        {
            //var host = "unix:///var/run/docker.sock"; //from inside container
            var host = "unix:///var/run/docker.sock";
            var network = "appnet";
            var outFile = "/etc/nginx/nginx.conf";
            var sleepTime = 5000;

            //Load the config once for initial settings
            await LoadConfig(host, network, outFile);

            //Start nginx
            var processStartInfo = new ProcessStartInfo("nginx");
            using (var process = Process.Start(processStartInfo))
            {
                while (true)
                {
                    if (await LoadConfig(host, network, outFile))
                    {
                        using (var reload = Process.Start("nginx", "-s reload"))
                        {
                            reload.WaitForExit();
                        }
                    }

                    Thread.Sleep(sleepTime);
                }
            }
        }

        private static async Task<bool> LoadConfig(string host, string network, string outFile)
        {
            var config = new DockerClientConfiguration(new Uri(host));
            var client = config.CreateClient();

            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters());

            var networkContainers = containers.Where(i => i.NetworkSettings.Networks.ContainsKey(network));
            var threaxNginxLabeled = networkContainers.Where(i => i.GetThreaxHost() != null)
                .Select(i => new ContainerNetworkInfo
                {
                    Host = i.GetThreaxHost(),
                    Port = i.GetThreaxPort(),
                    Name = i.Names.FirstOrDefault(),
                    Image = i.Image,
                    Ip = i.NetworkSettings.Networks[network].IPAddress,
                    MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                });

            var configWriter = new NginxConfWriter();
            var nginxConfig = configWriter.GetConfig(threaxNginxLabeled);

            bool updateFile;
            using(var reader = new StreamReader(File.Open(outFile, FileMode.OpenOrCreate)))
            {
                var currentFile = await reader.ReadToEndAsync();
                updateFile = currentFile != nginxConfig;
            }

            if (updateFile)
            {
                Console.WriteLine("New mappings found.");
                foreach (var item in threaxNginxLabeled)
                {
                    Console.WriteLine($"Mapping {item.Host} to {item.Ip} for container {item.Name} from {item.Image}");
                }

                using (var writer = new StreamWriter(File.Open(outFile, FileMode.Create)))
                {
                    await writer.WriteAsync(nginxConfig);
                }
            }

            return updateFile;
        }
    }
}
