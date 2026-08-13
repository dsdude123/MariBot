# Dev container for building and testing MariBot.
#
# The .NET version here must track the TargetFramework in the csproj files and
# the DOTNET_VERSION arg in MariBot-Core/Dockerfile — building net8.0 with a
# newer-only SDK on the host is exactly the problem this image exists to avoid.
ARG DOTNET_VERSION=8.0
FROM mcr.microsoft.com/devcontainers/dotnet:1-${DOTNET_VERSION}

# The base image ships a yarn apt source whose signing key has since rotated,
# which fails `apt-get update` outright. Nothing here needs yarn, so drop it.
RUN rm -f /etc/apt/sources.list.d/yarn.list

# Same native dependencies as the MariBot-Core runtime image, so the bot can
# actually be run from inside the container and not just compiled:
#   ffmpeg + python3           -> YoutubeDLSharp / RadioModule
#   libsodium23 + libopus0     -> Discord voice
#   libfontconfig1 + libfreetype6 -> SkiaSharp (latex rendering)
#   ca-certificates            -> HTTPS to third-party APIs
#   curl                       -> probing APIs by hand during development
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

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
