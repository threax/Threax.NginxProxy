using System;
using System.Collections.Generic;
using System.Text;

namespace DockerClient
{
    public class ContainerNetworkInfo
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Ip { get; set; }
        public String MaxBodySize { get; set; }
    }
}
