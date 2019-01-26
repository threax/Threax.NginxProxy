using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DockerClient
{
    public static class ContainerListResponseExtensions
    {
        public static String GetThreaxHost(this ContainerListResponse response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.host");
        }

        public static String GetThreaxPort(this ContainerListResponse response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.port");
        }

        public static String GetThreaxMaxBodySize(this ContainerListResponse response, bool throwOnMissing = false)
        {
            return ReadLabel(response, throwOnMissing, "threax.nginx.maxbodysize");
        }

        private static string ReadLabel(ContainerListResponse response, bool throwOnMissing, string label)
        {
            if (response.Labels.TryGetValue(label, out var result))
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
