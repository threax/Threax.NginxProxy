FROM nginx AS base
WORKDIR /app
EXPOSE 80

FROM threax/build AS build
WORKDIR /src
COPY . .
RUN dotnet restore ConfigWriter.csproj
RUN dotnet build ConfigWriter.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ConfigWriter.csproj -c Release -r debian-x64 --self-contained true -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
CMD ["/app/ConfigWriter"]