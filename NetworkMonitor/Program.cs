using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    class Program
    {
        static bool showConfig;
        static DockerClientConfiguration config;
        static DockerClient client;
        static MD5 md5 = MD5.Create();
        static DockerEventListener dockerEventListener;

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Threax.NetworkMonitor.");

                var host = "unix:///var/run/docker.sock";
                var network = "appnet";
                var outFile = "/data/config/nginx.conf";
                bool swarmMode = false;
                bool.TryParse(Environment.GetEnvironmentVariable("THREAX_NGINX_SWARM_MODE") ?? "true", out swarmMode);
                bool.TryParse(Environment.GetEnvironmentVariable("THREAX_NGINX_SHOW_CONFIG") ?? "false", out showConfig);

                if (swarmMode)
                {
                    Console.WriteLine("Using docker swarm mode with aliases.");
                }
                else
                {
                    Console.WriteLine("Using local containers with ips.");
                }

                config = new DockerClientConfiguration(new Uri(host));
                client = config.CreateClient();

                //Load the config once for initial settings
                await LoadConfig(host, network, outFile, swarmMode);

                dockerEventListener = new DockerEventListener(network);
                dockerEventListener.Start();

                //Start polling for changes
                var evt = await dockerEventListener.GetNextTask();
                while (evt != DockerEvent.ProcessEnded)
                {
                    Console.WriteLine("Got docker event " + evt);
                    await LoadConfig(host, network, outFile, swarmMode);
                    evt = await dockerEventListener.GetNextTask();
                }
            }
            finally
            {
                dockerEventListener.Dispose();
                md5?.Dispose();
                config?.Dispose();
                client?.Dispose();
            }
        }

        private static async Task<bool> LoadConfig(string host, string network, string outFile, bool swarmMode)
        {
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

            //Ensure output directory exists
            var directory = Path.GetDirectoryName(outFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //Write out any certs that need updating
            var certDir = Path.Combine(directory, "certs");
            if (!Directory.Exists(certDir))
            {
                Directory.CreateDirectory(certDir);
            }

            var nginxConfig = configWriter.GetConfig(containers);

            if(!File.Exists(outFile))
            {
                using (var stream = File.Open(outFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) { }
            }

            bool updateFile = false;
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

                if (showConfig)
                {
                    Console.WriteLine(nginxConfig);
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
                        UseHttps = i.GetThreaxUseHttps(),
                        MaxBodySize = "0" //i.GetThreaxMaxBodySize() //For now have to use 0 here, doesnt work otherwise
                    };
                });
        }
    }
}
