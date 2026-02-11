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
- Workers register via `worker-config.json` with HTTP endpoints
- `WorkerManagerService` tracks worker status (Ready/Offline/Busy) with health checks
- Job dispatch runs on a 1-second timer interval
- REST API controllers: `WorkerController`, `WorkerJobController` (Core); `WorkerController` (Worker)

### Data Layer
- **LiteDB** embedded NoSQL database (`data.db`) for Discord messages, chat history, static responses
- `DataService` manages all database operations with collections per guild

### Key Services (MariBot-Core/Services/)
- `OpenAiService` — OpenAI/ChatGPT integration
- `ImageService` — Image processing coordination
- `DynamicConfigService` — Runtime feature flags (`dynamic-config.json`)
- `BooruService`, `TwitterService`, `WikipediaService`, `GoogleService` — Third-party API wrappers
- `StaticTextResponseService` — Configurable canned responses

### Configuration
- `appsettings.json` — Discord token, API keys (Google, OpenAI, Wolfram Alpha, Twitter, etc.)
- `dynamic-config.json` — Runtime feature toggles (no restart needed)
- `worker-config.json` — Worker endpoint configuration

## Tech Stack Highlights
- **Discord**: Discord.Net 3.x (Commands + Interactions/slash commands)
- **Image Processing**: Magick.NET (ImageMagick), OpenCvSharp4
- **AI**: OpenAI client v2.5.0, Grok SDK, Black Forest Labs (Flux)
- **Database**: LiteDB 5.x
- **Logging**: Serilog with file sink
- **Testing**: XUnit 2.4.2