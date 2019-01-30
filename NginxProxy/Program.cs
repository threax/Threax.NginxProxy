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
        static async Task Main(string[] args)
        {
            var host = "unix:///var/run/docker.sock";
            var network = "appnet";
            var outFile = "/etc/nginx/nginx.conf";
            var sleepTime = 5000;
            bool swarmMode = false;
            bool.TryParse(Environment.GetEnvironmentVariable("THREAX_NGINX_SWARM_MODE") ?? "false", out swarmMode);

            if (swarmMode)
            {
                Console.WriteLine("Using docker swarm mode with aliases.");
            }

            //Load the config once for initial settings
            await LoadConfig(host, network, outFile, swarmMode);

            //Start nginx
            var processStartInfo = new ProcessStartInfo("nginx");
            using (var process = Process.Start(processStartInfo))
            {
                while (true)
                {
                    if (await LoadConfig(host, network, outFile, swarmMode))
                    {
                        Console.WriteLine("Reloading nginx.");
                        using (var reload = Process.Start("nginx", "-s reload"))
                        {
                            reload.WaitForExit();
                        }
                    }

                    Thread.Sleep(sleepTime);
                }
            }
        }

        private static async Task<bool> LoadConfig(string host, string network, string outFile, bool swarmMode)
        {
            var config = new DockerClientConfiguration(new Uri(host));
            var client = config.CreateClient();
            IEnumerable<ContainerNetworkInfo> containers;
            if (swarmMode)
            {
                containers = await GetSwarmContainers(network, client);
            }
            else
            {
                containers = await GetContainers(network, client);
            }

            var configWriter = new NginxConfWriter();
            var nginxConfig = configWriter.GetConfig(containers);

            bool updateFile;
            using (var reader = new StreamReader(File.Open(outFile, FileMode.OpenOrCreate)))
            {
                var currentFile = await reader.ReadToEndAsync();
                updateFile = currentFile != nginxConfig;
            }

            if (updateFile)
            {
                Console.WriteLine("New mappings found.");
                foreach (var item in containers)
                {
                    Console.WriteLine($"Mapping {item.ExternalHost} to {item.InternalHost} with port {item.InternalPort} for container {item.Name} from {item.Image}");
                }

                using (var writer = new StreamWriter(File.Open(outFile, FileMode.Create)))
                {
                    await writer.WriteAsync(nginxConfig);
                }
            }

            return updateFile;
        }

        private static async Task<IEnumerable<ContainerNetworkInfo>> GetContainers(string network, Docker.DotNet.DockerClient client)
        {
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters());
            var networkContainers = containers.Where(i => i.NetworkSettings.Networks.ContainsKey(network));
            return networkContainers.Where(i => i.GetThreaxHost() != null)
                .Select(i => new ContainerNetworkInfo
                {
                    ExternalHost = i.GetThreaxHost(),
                    InternalPort = i.GetThreaxPort(),
                    Name = i.Names.FirstOrDefault(),
                    Image = i.Image,
                    InternalHost = i.NetworkSettings.Networks[network].IPAddress,
                    MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                });
        }

        private static async Task<IEnumerable<ContainerNetworkInfo>> GetSwarmContainers(string network, Docker.DotNet.DockerClient client)
        {
            var networkInfo = await client.Networks.InspectNetworkAsync(network);
            var services = await client.Swarm.ListServicesAsync();
            var networkServices = services.Where(i => i.Spec.TaskTemplate.Networks.Any(n => n.Target == networkInfo.ID));

            return networkServices.Where(i => i.GetThreaxHost() != null)
                .Select(i =>
                {
                    //Use specified alias if there is one, otherwise use the first one from the network's settings
                    var internalHost = i.GetThreaxAlias() ?? i.Spec.TaskTemplate.Networks.Where(n => n.Target == networkInfo.ID).First().Aliases.First();
                    return new ContainerNetworkInfo
                    {
                        ExternalHost = i.GetThreaxHost(),
                        InternalPort = i.GetThreaxPort(),
                        Name = i.Spec.Name,
                        Image = i.Spec.TaskTemplate.ContainerSpec.Image,
                        InternalHost = internalHost,
                        MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                    };
                });
        }
    }
}
