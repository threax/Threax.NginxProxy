using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkMonitor
{
    public class ContainerNetworkInfo
    {
        public string ExternalHost { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string InternalHost { get; set; }
        public string InternalPort { get; set; }
        public String MaxBodySize { get; set; }
    }
}
