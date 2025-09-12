FROM jellyfin/jellyfin:latest AS base

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Jellyfin.sln
RUN dotnet build Jellyfin.sln -c Release --no-restore \
    -p:TreatWarningsAsErrors=false \
    -p:NoWarn=1591 \
    -p:RunAnalyzersDuringBuild=false

RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj -c Release -o /app/publish --no-restore \
    -p:TreatWarningsAsErrors=false \
    -p:NoWarn=1591 \
    -p:RunAnalyzersDuringBuild=false

FROM base
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Jellyfin.Server.dll"]