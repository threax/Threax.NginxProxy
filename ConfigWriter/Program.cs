using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System;
using System.Buffers.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DockerClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var host = "unix:///var/run/docker.sock"; //from inside container
            var host = "http://localhost:2375"; //insecure
            var network = "appnet";

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

            using(var writer = new StreamWriter(File.Open("C:/Development/DockerAppServer/nginx/nginx.conf", FileMode.Create)))
            {
                await writer.WriteAsync(nginxConfig);
            }
        }
    }
}
