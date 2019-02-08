using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkMonitor
{
    public static class SwarmServiceExtensions
    {
        public static String GetThreaxHost(this SwarmService response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.host");
        }

        public static String GetThreaxPort(this SwarmService response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.port");
        }

        public static String GetThreaxMaxBodySize(this SwarmService response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.maxbodysize");
        }

        public static String GetThreaxAlias(this SwarmService response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.alias");
        }

        public static bool GetThreaxUseHttps(this SwarmService response, bool throwOnMissing = false)
        {
            var val = ReadLabel(response, throwOnMissing, "threax.nginx.https");
            return String.IsNullOrWhiteSpace(val) ? false : bool.Parse(val);
        }

        private static string ReadLabel(SwarmService response, bool throwOnMissing, string label)
        {
            if (response.Spec.TaskTemplate.ContainerSpec.Labels.TryGetValue(label, out var result))
            {
                return result;
            }
            else if (throwOnMissing)
            {
                throw new Exception($"Cannot find label {label}.");
            }

            return null;
        }
    }
}
