# Multi-stage build for Jellyfin backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set working directory
WORKDIR /src

# Copy backend project files
COPY backend/ .

# Restore dependencies
RUN dotnet restore

# Publish the application (disable strict code analysis for Docker build)
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --property:TreatWarningsAsErrors=false \
    --property:GenerateDocumentationFile=false \
    --property:RunAnalyzersDuringBuild=false \
    --verbosity minimal

# ---------------------------
# Build frontend with Bun
# ---------------------------
FROM oven/bun:1 AS frontend-build

# Install Git and other system dependencies
RUN apt-get update && apt-get install -y \
    git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy lockfile + package.json
COPY frontend/package.json frontend/bun.lock ./

# Install deps (production only if you don't need dev)
RUN bun install --frozen-lockfile

# Install CLDR data required by globalize package
RUN bun add cldr-data

# Copy source
COPY frontend/ .

# Optional: copy git metadata if required
COPY .git/ ./.git/

# Run production build
RUN bun run build:production

# ---------------------------
# Runtime stage
# ---------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install required packages for media processing
RUN apt-get update && apt-get install -y \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published backend
COPY --from=build /app/publish .

# Copy built frontend
COPY --from=frontend-build /app/dist /app/jellyfin-web

# Create config + cache dirs
RUN mkdir -p /config /cache

# Expose Jellyfin ports
EXPOSE 8096
EXPOSE 8920

# Environment
ENV JELLYFIN_FFmpegPath=/usr/bin/ffmpeg \
    JELLYFIN_ServiceName=jellyfin \
    JELLYFIN_DataDir=/config \
    JELLYFIN_CacheDir=/cache \
    JELLYFIN_LogDir=/config/log \
    JELLYFIN_WebDir=/app/jellyfin-web \
    JELLYFIN_HOST_WEB_CLIENT=true

ENTRYPOINT ["dotnet", "jellyfin.dll"]