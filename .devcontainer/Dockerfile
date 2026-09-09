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

# Same native dependencies as the runtime images, so both services can actually
# be run from inside the container and not just compiled:
#   ffmpeg + python3           -> YoutubeDLSharp / RadioModule
#   libsodium23 + libopus0     -> Discord voice
#   libfontconfig1 + libfreetype6 -> SkiaSharp (latex rendering)
#   libgomp1                   -> Magick.NET's OpenMP build. Required to run
#                                 MariBot-Worker.Tests at all, not just the app:
#                                 without it every image test dies on a type
#                                 initializer.
#   fonts-noto-*               -> what FontCatalog scores captions against
#   ca-certificates            -> HTTPS to third-party APIs
#   curl                       -> probing APIs by hand during development
#
# OpenCV is the one thing not covered here. MariBot.Worker's face detection needs
# a libOpenCvSharpExtern.so linked against Ubuntu sonames Debian does not carry,
# which is why MariBot.Worker/Dockerfile is built on jammy — see the note there.
# Everything except deepfry works without it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        ffmpeg \
        fonts-noto-cjk \
        fonts-noto-core \
        libfontconfig1 \
        libfreetype6 \
        libgomp1 \
        libopus0 \
        libsodium23 \
        python3 \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
