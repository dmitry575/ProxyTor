## **Http proxy server via Tors**

The Tor server uses socks 5 as type of proxy, and if you want to use it as http-proxy you need to camber the request.
ProxyTor just receives an http request and redirects it through the tors instances.
At what a lot of tor servers are raised and each new request coming to the http proxy is redirected through different tor instances

## Setting example

All settings can be made through the `docker-compose.yml` file

`
  tor_servers:
    image: tors_image:latest
    build:
      context: tors/
      dockerfile: Dockerfile
    environment:
      - TOR_INSTANCES=30
      - TOR_PORT_BASE=9000
    ports:
      - 9000-9029:9000-9029

  proxy_tor:
    build:
      context: src/
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_Logging__LogLevel__Default=Debug
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_Port=5008
      - ASPNETCORE_Tor__hostname=tor_servers
      - ASPNETCORE_portFrom=9000
      - ASPNETCORE_portTo=9029
    depends_on:
      - tor_servers
    ports:
      - 5008:5008
`

TOR_INSTANCES - number of instances to start
TOR_PORT_BASE - port number from which the launch starts

When changing the `TOR_INSTANCES` and `TOR_PORT_BASE` parameters, don't forget to fix the section:
`
    ports:
      - 9000-9029:9000-9029
`

ASPNETCORE_Port - port number on which the HTTP proxy will work
ASPNETCORE_portFrom - initial server port
ASPNETCORE_portTo - destination port of servers

## Run in docker

If you are running on `windows`, you only need to run the `docker-proxy.cmd` file, for other operating systems using `docker-compose`:
`
docker-compose -f docker-compose.yml build proxy_tor
docker-compose -f docker-compose.yml up -d
`

You can use `curl` to check if it works
`
curl -k -x http://localhost:5008 https://api.ipify.org?format=json
`
The result will be tor IP address. If you run it several times, then the IP addresses will change.