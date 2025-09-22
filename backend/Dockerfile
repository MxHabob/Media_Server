# Multi-stage build for Jellyfin backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY backend/ .
RUN dotnet restore
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj --configuration Release --no-restore --output /app/publish --property:TreatWarningsAsErrors=false --property:GenerateDocumentationFile=false --property:RunAnalyzersDuringBuild=false --verbosity minimal

# Build frontend
FROM node:20 AS frontend-build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm cache clean --force
RUN npm install
COPY frontend/ .
COPY .git/ ./.git/
RUN npm run build:development
RUN ls -la /app/dist/
RUN find /app/dist/ -type f

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend-build /app/dist/ /app/jellyfin-web/
RUN ls -la /app/jellyfin-web/
RUN find /app/jellyfin-web/ -type f
RUN chmod -R 755 /app/jellyfin-web/
RUN mkdir -p /config /cache
EXPOSE 8096
EXPOSE 8920
ENV JELLYFIN_FFmpegPath=/usr/bin/ffmpeg
ENV JELLYFIN_ServiceName=jellyfin
ENV JELLYFIN_DataDir=/config
ENV JELLYFIN_CacheDir=/cache
ENV JELLYFIN_LogDir=/config/log
ENV JELLYFIN_WebDir=/app/jellyfin-web
ENV JELLYFIN_HOST_WEB_CLIENT=true
ENTRYPOINT ["dotnet", "jellyfin.dll"]