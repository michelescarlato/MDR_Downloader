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

ARG PUID=1000
ARG PGID=1000

# Create user/group matching host IDs
RUN groupadd -g "${PGID}" mdr \
 && useradd  -u "${PUID}" -g "${PGID}" -m -s /usr/sbin/nologin mdr

# App binaries
COPY --from=build /app/out /app/

RUN ls -la /app && test -f /app/MDR_Downloader.dll

# Create data dirs (use volumes for real data)
RUN mkdir -p /app/MDR_Data /app/MDR_Sources /app/test /app/MDR_Sources /app/biolincc /app/ctg /app/euctr /app/isrctn /app/pubmed /app/who /app/yoda \
 && chown -R "${PUID}:${PGID}" /app

# If the base image includes the non-root 'app' user (common in recent dotnet images), use it:
USER mdr

WORKDIR /app/MDR_Data

ENTRYPOINT ["dotnet", "/app/MDR_Downloader.dll"]
