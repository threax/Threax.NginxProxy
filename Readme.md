# Threax.NginxProxy
This is a program that monitors docker for new containers on a network and updates the nginx.conf to include those entries.

## How to Use
### Create Secrets
Create a secret called public.pem and one called private.pem to be your public and private keys.

### Build
Run the following to build. 
```
docker-compose build
```

Next choose if you want to run in docker-compose mode or swarm mode.

### docker-compose Mode
Create a network
```
docker network create appnet
```
Start the service
```
docker-compose up -d
```
Or call it without -d to see the logs. This is the best way to see what the app is doing.

### Swarm Mode
Create a network
```
docker network create -d overlay --attachable appnet
```
Start the service
```
docker stack deploy -c docker-compose.yml proxy
```

## Connecting apps
Add a network entry you your docker-compose.yml file
```
networks:
  appnet:
    external: true
```

Reference this network from your service
```
    networks:
      - appnet
```

Add the appropriate labels, you need to have threax.nginx.host, but can leave off threax.nginx.port.
```
    labels:
      - "threax.nginx.host=www.example.com"
      - "threax.nginx.port=5000"
```

## Additional Config
You will need to point the url for host to your swarm so it can serve your apps.

## Example
If you are creating one of my .Net Core template apps, this is a good complete config example to use.
```
version: '3.4'

services:
  app:
    image: threax.example
    build:
      context: Example
      dockerfile: Dockerfile
    # Make sure to connect to appnet
    networks:
      - appnet
    volumes:
      - type: bind
        source: ../DevData/example
        target: /appdata
    # Set the app to use port 5000
    environment:
      - ASPNETCORE_URLS=http://*:5000
    # Run as normal user 1000
    user: '1000'
    # Run example.com and let the proxy know to check port 5000 as set above
    labels:
      - "threax.nginx.host=www.example.com"
      - "threax.nginx.port=5000"

networks:
  appnet:
    external: true
```