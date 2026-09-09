# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MariBot (formerly StarBot) is a Discord bot built on .NET 8.0 with a distributed architecture. The Core bot handles Discord interactions and delegates compute-heavy operations (image processing, AI inference) to a separate Worker service via HTTP REST API.

**External dependency**: SharpTalkGenerator (https://github.com/dsdude123/SharpTalkGenerator)

## Build & Test Commands

```bash
# Build entire solution
dotnet build MariBot.sln

# Run all tests
dotnet test MariBot.sln

# Run specific test project
dotnet test MariBot-Common.Tests
dotnet test MariBot-Worker.Tests

# Run a single test
dotnet test MariBot-Worker.Tests --filter "FullyQualifiedName~TestMethodName"
```

These need the .NET 8 SDK. `.devcontainer/` provides one along with the native
dependencies from the runtime images, so the toolchain does not have to be installed on
the host.

Every test project runs on Windows and Linux alike. On Linux, Magick.NET's OpenMP
build needs `libgomp1` present or every image test fails on a type initializer; the
dev container and the CI job both install it.

Paths in `MariBot.Worker` must go through `WorkerPaths`, and font and Haar cascade
locations through `FontCatalog` and `OpenCVHandler`'s configuration key. A literal
`"\\"` or a `C:\` default is what kept the worker Windows-only.

`WorkerGlobals.Job` is an `AsyncLocal`, not a plain static. A job that gets abandoned
keeps running — Magick.NET and OpenCV calls are not cancellable — so a shared static
would have it write its result over whatever job the worker picked up next.

## Solution Structure

| Project | Purpose |
|---|---|
| **MariBot-Core** | Main ASP.NET Core app — Discord bot, command routing, 20+ service integrations |
| **MariBot.Worker** | ASP.NET Core worker service for GPU-accelerated image/AI processing |
| **MariBot-Common** | Shared library (models, utilities) used by both Core and Worker |
| **MariBot-Data** | Data layer (minimal/legacy) |
| **MariBot-Common.Tests** | XUnit tests for Common library |
| **MariBot-Worker.Tests** | XUnit tests for Worker (image handler tests) |

## Architecture

### Command Processing Flow
1. Discord message/interaction → `DiscordBotService` (IHostedService)
2. `CommandHandlingService` routes to the appropriate module in `Modules/Text/`
3. For GPU operations: `WorkerManagerService` enqueues a `WorkerJob`
4. Worker service processes job via handlers (`MagickImageHandler`, `OpenCVHandler`, `StableDiffusionTextVariantHandler`, `EasyOcrHandler`)
5. Result returned to Discord

### Worker System
- Workers register themselves at `POST /api/workers/register` with a pre-shared key
  (`WorkerSettings:PreSharedKey`, same value on both sides) and heartbeat to stay
  registered. There is no worker list in configuration; a worker that stops checking
  in is marked Offline and then evicted
- `WorkerRegistry` owns the pool — membership, hold/ready, capability routing, and a
  per-worker `CircuitBreaker` (closed → open after repeated failures → half-open trial)
- `JobQueue` holds two strictly ordered lanes; a command declares which one it uses in
  `CommandRegistry`, alongside its capability and any per-command timeout
- `WorkerManagerService` runs the 1-second dispatch loop: evict stale workers, abandon
  jobs past their deadline, then place queued jobs high lane first
- `JobMetricsService` records every finished job to LiteDB; `/metrics` and
  `/metrics/json` report over a window
- REST API controllers: `WorkerRegistrationController`, `WorkerJobController`,
  `MetricsController`, `WorkerController` (Core); `WorkerController` (Worker)
- Only transport failures and timeouts count against a worker's breaker. A job that
  failed because the request was bad says nothing about the worker and must not take
  it out of rotation

### Data Layer
- **LiteDB** embedded NoSQL database (`data.db`) for Discord messages, chat history, static responses
- `DataService` manages all database operations with collections per guild

### Key Services (MariBot-Core/Services/)
- `OpenAiService` — OpenAI/ChatGPT integration
- `ImageService` — Image processing coordination
- `DynamicConfigService` — Runtime feature flags (`dynamic-config.json`)
- `BooruService`, `TwitterService`, `WikipediaService`, `GoogleService` — Third-party API wrappers
- `SleeperService` — Sleeper fantasy football (read-only public API, no key) plus the 15-minute transaction poller; leagues are bound per-guild via a LiteDB `SleeperSubscription`
- `SleeperPlayerCache` — Disk-backed daily copy of Sleeper's ~14 MB NFL player catalog, used to resolve player ids to names
- `StaticTextResponseService` — Configurable canned responses

### Configuration
- `appsettings.json` — Discord token, API keys (Google, OpenAI, Wolfram Alpha, Twitter, etc.),
  and the `WorkerSettings` section (pre-shared key, heartbeat and eviction timings, job
  timeout, circuit breaker thresholds, metrics retention)
- `dynamic-config.json` — Runtime feature toggles (no restart needed)
- The worker's own `WorkerSettings` section names the Core to register with, the address
  to advertise, and the capabilities it claims

## Tech Stack Highlights
- **Discord**: Discord.Net 3.x (Commands + Interactions/slash commands)
- **Image Processing**: Magick.NET (ImageMagick), OpenCvSharp4
- **AI**: OpenAI client v2.5.0, Grok SDK, Black Forest Labs (Flux)
- **Database**: LiteDB 5.x
- **Logging**: Serilog with file sink
- **Testing**: XUnit 2.4.2