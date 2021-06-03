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
using DiscordBot.Services;
using MariBot.Modules;
using MariBot.Services;
using UrbanDictionnet;
using System.Diagnostics;
using System.Threading;
using System.IO.Compression;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using ImageMagick;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static readonly string sharpTalkLink = "https://github.com/dsdude123/SharpTalkGenerator/releases/download/v1.0/SharpTalkGenerator.zip";
        public static readonly string waifuLabsLink = "https://github.com/dsdude123/WaifuLabs.NET/releases/download/v1.0/waifulabs.exe";

        public static DiscordSocketClient _client;
        public static IConfiguration _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _config = BuildConfig();
            _client.Disconnected += ResetBot;

            var services = ConfigureServices();
            var logService = services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

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

                await _client.LoginAsync(TokenType.Bot, _config["token"]);
            }
            catch (HttpRequestException e)
            {
                ResetBot(e);
            }

            await _client.StartAsync();

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
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                // Logging
                .AddLogging()
                .AddSingleton<LogService>()
                // Extra
                .AddSingleton(_config)
                // Add additional services here...
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<HttpService>()
                .AddSingleton<XmlDocument>()
                .AddSingleton<UrbanDictionaryService>()
                .AddSingleton<UrbanClient>()
                .AddSingleton<WikipediaService>()
                .AddSingleton<MediawikiSharp_API.Mediawiki>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<StaticTextResponseService>()
                .AddSingleton<BooruService>()
                .AddSingleton<GoogleService>()
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