using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkMonitor
{
    public class NginxConfWriter
    {
        public String GetConfig(IEnumerable<ContainerNetworkInfo> networkInfos)
        {
            var result = new StringBuilder($@"
 
events {{ worker_connections 1024; }}
 
http {{
    sendfile on;
    server_tokens off;

    # Fix identityserver bad gateway errors
    proxy_buffer_size   128k;
    proxy_buffers   4 256k;
    proxy_busy_buffers_size   256k;
    large_client_header_buffers 4 16k;
    
    # Compression
    gzip on;
    gzip_types      application/javascript text/css;
    #gzip_proxied    any; # The docs say you need this, but it doesnt do anything

    # Proxy Settings
    resolver 127.0.0.11 ipv6=off valid=30s;         #docker embedded dns ip
    proxy_redirect     off;
    proxy_set_header   Host $host;
    proxy_set_header   X-Real-IP $remote_addr;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Host $server_name;
    proxy_set_header   X-Forwarded-Proto $scheme;
    proxy_set_header   X-Forwarded-Port $server_port;

    # SSL Settings
    # https://mozilla.github.io/server-side-tls/ssl-config-generator/
    # Based on modern
    ssl_protocols TLSv1.2;
    ssl_ciphers 'ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-SHA384:ECDHE-RSA-AES256-SHA384:ECDHE-ECDSA-AES128-SHA256:ECDHE-RSA-AES128-SHA256';
    ssl_prefer_server_ciphers on;

    # Stuff to try to fix 504 errors
    #https://stackoverflow.com/questions/44635169/configure-identityserver4-behind-nginx-reverse-proxy
    #proxy_http_version 1.1;
    #proxy_set_header Upgrade $http_upgrade;
    #proxy_set_header Connection keep-alive;
    #proxy_cache_bypass $http_upgrade;
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
            set $upstream      {host};");



                if (networkInfo.Cert != null)
                {
                    result.Append($@"
            proxy_pass         https://$upstream;
            # https://docs.nginx.com/nginx/admin-guide/security-controls/securing-http-traffic-upstream/
            # This enables ssl to work from target containers, would have to call https above
            proxy_ssl_trusted_certificate {networkInfo.Cert};
            proxy_ssl_session_reuse on;
            # proxy_ssl_verify              off;
            # proxy_ssl_server_name         on;
");
                }
                else
                {
                    result.Append($@"
            proxy_pass         http://$upstream;");
                }

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
