using System;
using System.Collections.Generic;
using System.Text;

namespace DockerClient
{
    public class NginxConfWriter
    {
        public String GetConfig(IEnumerable<ContainerNetworkInfo> networkInfos)
        {
            var result = new StringBuilder($@"
 
events {{ worker_connections 1024; }}
 
http {{
    sendfile on;

    # fix identityserver bad gateway errors
    proxy_buffer_size   128k;
    proxy_buffers   4 256k;
    proxy_busy_buffers_size   256k;
    large_client_header_buffers 4 16k;
    
    #Compression
    gzip on;
    gzip_types      application/javascript text/css;
    #gzip_proxied    any; # The docs say you need this, but it doesnt do anything
");
            foreach(var networkInfo in networkInfos)
            {
                var host = networkInfo.InternalHost;
                if(networkInfo.InternalPort != null)
                {
                    host += ":" + networkInfo.InternalPort;
                }

                result.Append($@"
server {{
        listen 80;
		listen                443 ssl;
		ssl_certificate       /run/secrets/public.pem;
		ssl_certificate_key   /run/secrets/private.pem;

		server_name {networkInfo.ExternalHost};
 
        location / {{
            resolver 127.0.0.11 ipv6=off;			#docker embedded dns ip
            set $upstream {host};
            proxy_pass         http://$upstream;
            proxy_redirect     off;
            proxy_set_header   Host $host;
            proxy_set_header   X-Real-IP $remote_addr;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Host $server_name;
            proxy_set_header   X-Forwarded-Proto $scheme;
            proxy_set_header   X-Forwarded-Port $server_port;
            # This enables ssl to work from target containers, would have to call https above
            #proxy_ssl_trusted_certificate /etc/sslbackend/localhost.cert;
            #proxy_ssl_verify       off;
            #proxy_ssl_server_name  on;");

                if(networkInfo.MaxBodySize != null)
                {
                    result.Append($@"
            client_max_body_size {networkInfo.MaxBodySize};
");
                }

                result.Append(@"
        }
    }");
            }

            result.Append(@"
}");

            result.Replace("\r", "");
            return result.ToString();
        }
    }
}
