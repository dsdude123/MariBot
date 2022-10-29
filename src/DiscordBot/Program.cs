using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MariBot.Modules;
using MariBot.Services;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using ImageMagick;
using Discord.Interactions;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3;

namespace MariBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static readonly string sharpTalkLink = "https://github.com/dsdude123/SharpTalkGenerator/releases/download/v1.0/SharpTalkGenerator.zip";
        public static readonly string waifuLabsLink = "https://github.com/dsdude123/WaifuLabs.NET/releases/download/v2.0/waifulabs.exe";

        public static DiscordSocketClient client;
        public static IConfiguration config;

        public async Task MainAsync()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            client = new DiscordSocketClient(clientConfig);
            config = BuildConfig();
            client.Disconnected += ResetBot;

            var services = ConfigureServices();
            var logService = services.GetRequiredService<LogService>();
            var interactionService = services.GetRequiredService<InteractionService>();

            client.Ready += async () =>
            {
                await interactionService.RegisterCommandsGloballyAsync(true);
            };

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            OpenCL.IsEnabled = true;

            if(!OpenCL.IsEnabled)
            {
                await logService.LogInit(new LogMessage(LogSeverity.Warning, "OpenCL", "OpenCL is not enabled."));
            } else
            {
                await logService.LogInit(new LogMessage(LogSeverity.Info, "OpenCL", "OpenCL is enabled."));
                foreach(OpenCLDevice device in OpenCL.Devices)
                {
                    device.IsEnabled = true;
                    await logService.LogInit(new LogMessage(LogSeverity.Info, "OpenCL", $"{device.DeviceType}: {device.Name} | Enabled: {device.IsEnabled} | Score: {device.BenchmarkScore}"));         
                }
            }

            try
            {
                await logService.LogInit(new LogMessage(LogSeverity.Info, "Startup", "Updating ffmepg..."));
                try
                {
                    FFmpeg.SetExecutablesPath(".");
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                } catch (Exception ex)
                {
                    await logService.LogInit(new LogMessage(LogSeverity.Error, "Startup", "Failed to update ffmpeg!", ex));
                }

                if (!File.Exists("SharpTalkGenerator.exe"))
                {
                    await logService.LogInit(new LogMessage(LogSeverity.Warning, "Startup", "SharpTalkGenerator is missing. Installing..."));
                    try
                    {
                        HttpClient sharpTalkDownloadClient = new HttpClient();
                        HttpResponseMessage responseMessage = sharpTalkDownloadClient.GetAsync(sharpTalkLink).Result;
                        responseMessage.EnsureSuccessStatusCode();
                        byte[] sharpTalkPackage = responseMessage.Content.ReadAsByteArrayAsync().Result;
                        File.WriteAllBytes("sharptalk.zip", sharpTalkPackage);
                        ZipFile.ExtractToDirectory("sharptalk.zip", Environment.CurrentDirectory);
                        File.Delete("sharptalk.zip");
                    }
                    catch (Exception ex)
                    {
                        await logService.LogInit(new LogMessage(LogSeverity.Error, "Startup", "Failed to install SharpTalkGenerator! DecTalk TTS command will be unavailable.", ex));
                    }
                }

                if (!File.Exists("waifulabs.exe"))
                {
                    await logService.LogInit(new LogMessage(LogSeverity.Warning, "Startup", "WaifuLabs.NET is missing. Installing..."));
                    Console.Write("");
                    try
                    {
                        HttpClient waifulabsDownloadClient = new HttpClient();
                        HttpResponseMessage responseMessage = waifulabsDownloadClient.GetAsync(waifuLabsLink).Result;
                        responseMessage.EnsureSuccessStatusCode();
                        byte[] waifulabsExecutable = responseMessage.Content.ReadAsByteArrayAsync().Result;
                        File.WriteAllBytes("waifulabs.exe", waifulabsExecutable);
                    }
                    catch (Exception ex)
                    {
                        await logService.LogInit(new LogMessage(LogSeverity.Error, "Startup", "Failed to install WaifuLabs.NET. Waifu command will be unavailable.", ex));
                    }
                }

                await logService.LogInit(new LogMessage(LogSeverity.Info, "Startup", "Updating youtube-dl..."));
                UpdateYouTubeDl();

                await client.LoginAsync(TokenType.Bot, config["token"]);
            }
            catch (HttpRequestException e)
            {
                ResetBot(e);
            }

            await client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task ResetBot(Exception ex)
        {
            Console.WriteLine("Bot reset event has been triggered, resetting in 5 seconds: " + ex.Message);
            Thread.Sleep(5000);
            System.Diagnostics.Process.Start(Assembly.GetExecutingAssembly().Location);
            Environment.Exit(59);           
        }

        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                // Base
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(config)
                // Add additional services here...
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<HttpService>()
                .AddSingleton<XmlDocument>()
                .AddSingleton<UrbanDictionaryService>()
                .AddSingleton<WikipediaService>()
                .AddSingleton<MediawikiSharp_API.Mediawiki>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<StaticTextResponseService>()
                .AddSingleton<BooruService>()
                .AddSingleton<GoogleService>()
                .AddSingleton<Edges2HentaiService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<TalkHubService>()
                .AddSingleton<ImageHubService>()
                .AddSingleton(x => new OpenAIService(new OpenAiOptions() { ApiKey = config["openAiApiKey"] }))
                .BuildServiceProvider();
        }

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }

        private void UpdateYouTubeDl()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"-U",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };

            process.Start();
            process.WaitForExit();
        }
    }
}