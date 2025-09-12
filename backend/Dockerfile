FROM jellyfin/jellyfin:latest AS base

# Build your custom backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/jellyfin  # Change WORKDIR to the jellyfin subdirectory

# Copy the entire source code, then navigate to the jellyfin folder
COPY . .
WORKDIR /src/jellyfin  # Ensure the context is set to the jellyfin folder

# Restore dependencies then build the solution
RUN dotnet restore Jellyfin.sln
RUN dotnet build Jellyfin.sln -c Release --no-restore \
    -p:TreatWarningsAsErrors=false \
    -p:NoWarn=1591 \
    -p:RunAnalyzersDuringBuild=false

# Publish the backend
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj -c Release -o /app/publish --no-restore \
    -p:TreatWarningsAsErrors=false \
    -p:NoWarn=1591 \
    -p:RunAnalyzersDuringBuild=false

# Final image
FROM base
WORKDIR /app

# Copy your custom build
COPY --from=build /app/publish .

# Use your custom entrypoint
ENTRYPOINT ["dotnet", "Jellyfin.Server.dll"]