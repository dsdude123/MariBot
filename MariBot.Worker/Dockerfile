# syntax=docker/dockerfile:1.7

# Build context is expected to be the repo root so that MariBot.Worker
# can reference the MariBot-Common project sibling.
#   docker build -f MariBot.Worker/Dockerfile -t maribot-worker .

# Ubuntu 22.04 rather than the default Debian, and not a free choice:
# OpenCvSharp4.official.runtime.linux-x64 ships one prebuilt
# libOpenCvSharpExtern.so linked against Ubuntu-era sonames (libtesseract.so.4,
# libavcodec.so.58, libtiff.so.5, libIlmImf-2_5.so.25). Debian 12 has none of
# them under those names, and the loader failure surfaces as an unhandled
# exception on the finalizer thread that takes the whole worker down.
ARG DOTNET_VERSION=8.0-jammy

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY MariBot-Common/MariBot.Common.csproj MariBot-Common/
COPY MariBot.Worker/MariBot.Worker.csproj MariBot.Worker/
RUN dotnet restore MariBot.Worker/MariBot.Worker.csproj

COPY MariBot-Common/ MariBot-Common/
COPY MariBot.Worker/ MariBot.Worker/
RUN dotnet publish MariBot.Worker/MariBot.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# Native dependencies:
#   libgomp1           -> Magick.NET's OpenMP build. Without it the native
#                         library is found but cannot load, and every single
#                         image command fails on a type initializer.
#   libtesseract4 .. libgtk2.0-0
#                      -> what libOpenCvSharpExtern.so is linked against. It is
#                         one prebuilt binary pulling in OpenCV's full optional
#                         surface (video, imaging codecs, GUI), so all of it has
#                         to be present even though the worker only classifies
#                         faces. `ldd` on that .so is the authority for this
#                         list; keep them together when bumping OpenCvSharp.
#   opencv-data        -> the Haar cascades OpenCVHandler classifies with
#   fonts-noto-*       -> the fonts FontCatalog scores text against: Latin, plus
#                         the CJK collection Windows keeps as four separate files
#   ca-certificates    -> HTTPS out
#   curl               -> the healthcheck below
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        fonts-noto-cjk \
        fonts-noto-core \
        libavcodec58 \
        libavformat58 \
        libdc1394-25 \
        libgomp1 \
        libgtk2.0-0 \
        libopenexr25 \
        libopenjp2-7 \
        libswscale5 \
        libtesseract4 \
        libtiff5 \
        opencv-data \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish/ ./

# /app/logs is a mount point with a working in-image default: the worker runs
# correctly with nothing mounted, and a volume only decides where the log file
# lives. Content, Python and temp deliberately stay inside the image — the
# first two ship with the build and the third is per-job scratch.
RUN mkdir -p /app/logs

# 8092 is the port MariBot-Core's worker-config.json addresses workers on.
# Results travel the other way to port 8091 on whatever address the worker saw
# the request arrive from, which is Core's own address, so nothing here has to
# know where Core is.
ENV ASPNETCORE_URLS=http://+:8092 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    MariBot__LogPath=/app/logs/maribot.log

EXPOSE 8092

# The same endpoint Core polls to decide whether this worker is Ready, so an
# unhealthy container and a worker Core has given up on mean the same thing.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://localhost:8092/api/worker || exit 1

ENTRYPOINT ["dotnet", "MariBot.Worker.dll"]
