# Multi-stage build for Jellyfin backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

# Copy backend project files
COPY backend/ .

# Restore dependencies
RUN dotnet restore

# Publish the application (disable strict code analysis for Docker build)
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj --configuration Release --no-restore --output /app/publish --property:TreatWarningsAsErrors=false --property:GenerateDocumentationFile=false --property:RunAnalyzersDuringBuild=false --verbosity minimal

# Build frontend
FROM node:20 AS frontend-build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
RUN npm install
COPY frontend/ .
COPY .git/ ./.git/
RUN npm run build:production

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install required packages for media processing
RUN apt-get update && apt-get install -y \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy built frontend
COPY --from=frontend-build /app/dist /app/jellyfin-web

# Create directories for config and cache
RUN mkdir -p /config /cache && chown -R 1000:1000 /config /cache

# Expose the standard Jellyfin ports
EXPOSE 8096
EXPOSE 8920

# Set environment variables
ENV JELLYFIN_FFmpegPath=/usr/bin/ffmpeg
ENV JELLYFIN_ServiceName=jellyfin
ENV JELLYFIN_DataDir=/config
ENV JELLYFIN_CacheDir=/cache
ENV JELLYFIN_LogDir=/config/log
ENV JELLYFIN_WebDir=/app/jellyfin-web
ENV JELLYFIN_HOST_WEB_CLIENT=true

# Run the application
ENTRYPOINT ["dotnet", "Jellyfin.Server.dll"]