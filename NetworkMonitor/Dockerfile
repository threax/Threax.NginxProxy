FROM microsoft/dotnet:2.2-runtime AS base
# The following lines will install some networking tools useful for debugging.
# RUN apt-get update
# RUN apt-get install iputils-ping dnsutils curl -y
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