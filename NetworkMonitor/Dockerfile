﻿FROM microsoft/dotnet:2.2-runtime AS base

# Install docker, use this if you want to enable the use of docker events
#WORKDIR /docker
#RUN apt-get update &&\
    #apt-get install wget -y &&\
    #apt-get install apt-transport-https -y &&\
    #wget -q https://download.docker.com/linux/static/stable/x86_64/docker-18.09.1.tgz &&\
    #tar xzvf docker-18.09.1.tgz &&\
    #mv docker/* /usr/bin/ &&\
    #rm docker-18.09.1.tgz &&\
    #apt-get remove --auto-remove apt-transport-https -y &&\
    #apt-get remove --auto-remove wget -y &&\
    #apt-get autoremove &&\
    #apt-get clean

WORKDIR /app

FROM threax/build AS build
WORKDIR /src
COPY . .
WORKDIR /src/NetworkMonitor
RUN dotnet restore NetworkMonitor.csproj
RUN dotnet build NetworkMonitor.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish NetworkMonitor.csproj -c Release -o /app

FROM base AS final
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "NetworkMonitor.dll"]