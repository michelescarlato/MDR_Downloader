# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy source from the repo checkout (no git clone needed)
COPY . .

# Restore + publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# App binaries
COPY --from=build /app/out ./

# Create data dirs (use volumes for real data)
RUN mkdir -p /data/MDR_Sources /data/biolincc /data/ctg /data/euctr /data/isrctn /data/pubmed /data/who /data/yoda

# If the base image includes the non-root 'app' user (common in recent dotnet images), use it:
# USER app

ENTRYPOINT ["dotnet", "MDR_Downloader.dll"]
