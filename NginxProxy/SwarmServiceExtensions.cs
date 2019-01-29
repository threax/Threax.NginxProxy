using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DockerClient
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
