FROM nginx AS base
WORKDIR /app

FROM threax/build AS build
WORKDIR /src
COPY . .
RUN dotnet restore NginxProxy.csproj
RUN dotnet build NginxProxy.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish NginxProxy.csproj -c Release -r linux-x64 --self-contained true -o /app

FROM base AS final
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
COPY --from=publish /app .
CMD ["/app/NginxProxy"]