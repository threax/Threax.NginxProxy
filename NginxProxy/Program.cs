﻿using Docker.DotNet;
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
            //var host = "unix:///var/run/docker.sock"; //from inside container
            var host = "unix:///var/run/docker.sock";
            var network = "appnet";
            var outFile = "/etc/nginx/nginx.conf";
            var sleepTime = 5000;
            bool swarmMode = false;
            bool.TryParse(Environment.GetEnvironmentVariable("THREAX_NGINX_SWARM_MODE") ?? "false", out swarmMode);

            if (swarmMode)
            {
                Console.WriteLine("Using docker swarm mode.");
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
                    Console.WriteLine($"Mapping {item.Host} with port {item.Port} to {item.Ip} for container {item.Name} from {item.Image}");
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
                    Host = i.GetThreaxHost(),
                    Port = i.GetThreaxPort(),
                    Name = i.Names.FirstOrDefault(),
                    Image = i.Image,
                    Ip = i.NetworkSettings.Networks[network].IPAddress,
                    MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                });
        }

        private static async Task<IEnumerable<ContainerNetworkInfo>> GetSwarmContainers(string network, Docker.DotNet.DockerClient client)
        {
            var networkInfo = await client.Networks.InspectNetworkAsync(network);
            var services = await client.Swarm.ListServicesAsync();
            var networkServices = services.Where(i => i.Endpoint.VirtualIPs.Any(ip => ip.NetworkID == networkInfo.ID));
            return networkServices.Where(i => i.GetThreaxHost() != null)
                .Select(i =>
                {
                    var ip = i.Endpoint.VirtualIPs.Where(n => n.NetworkID == networkInfo.ID).Select(j => j.Addr).First();
                    var slashIndex = ip.IndexOf('/');
                    if(slashIndex != -1)
                    {
                        ip = ip.Substring(0, slashIndex);
                    }
                    return new ContainerNetworkInfo
                    {
                        Host = i.GetThreaxHost(),
                        Port = i.GetThreaxPort(),
                        Name = i.Spec.Name,
                        Image = i.Spec.TaskTemplate.ContainerSpec.Image,
                        Ip = ip,
                        MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                    };
                });
        }
    }
}
