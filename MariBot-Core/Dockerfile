# syntax=docker/dockerfile:1.7

# Build context is expected to be the repo root so that MariBot-Core
# can reference the MariBot-Common project sibling.
#   docker build -f MariBot-Core/Dockerfile -t maribot-core .

ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY MariBot-Common/MariBot.Common.csproj MariBot-Common/
COPY MariBot-Core/MariBot.Core.csproj MariBot-Core/
RUN dotnet restore MariBot-Core/MariBot.Core.csproj

COPY MariBot-Common/ MariBot-Common/
COPY MariBot-Core/ MariBot-Core/
RUN dotnet publish MariBot-Core/MariBot.Core.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# Native dependencies:
#   ffmpeg + python3   -> YoutubeDLSharp / RadioModule
#   libsodium23 + libopus0 -> Discord voice
#   libfontconfig1 + libfreetype6 -> SkiaSharp (latex rendering)
#   ca-certificates    -> HTTPS to third-party APIs
#   curl               -> container healthchecks
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        ffmpeg \
        libfontconfig1 \
        libfreetype6 \
        libopus0 \
        libsodium23 \
        python3 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish/ ./

# Every one of these is a mount point with a working in-image default, so the
# app runs correctly with no configuration — mounting a volume over one only
# decides where the data lives, never whether it works.
# /app/config      -> writable config, incl. the rotating Yahoo OAuth token
# /app/data        -> LiteDB file + legacy text-response migration source
# /app/logs        -> Serilog file sink (maribot-*.log)
# /app/yt-downloads -> YoutubeDl output
RUN mkdir -p /app/config /app/data /app/logs /app/yt-downloads

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DiscordSettings__YouTubeDlOutputPath=/app/yt-downloads \
    DiscordSettings__DataDbPath=/app/data/data.db \
    DiscordSettings__YahooAuthPath=/app/config/yahoo-auth.json \
    MariBot__LogPath=/app/logs/maribot.log

EXPOSE 8080

ENTRYPOINT ["dotnet", "MariBot.Core.dll"]
