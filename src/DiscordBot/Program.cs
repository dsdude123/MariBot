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
using StarBot.Modules;
using StarBot.Services;
using UrbanDictionnet;
using System.Diagnostics;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient _client;
        public static IConfiguration _config;
        public static bool isSharpTalkPresent;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _config = BuildConfig();
            _client.Disconnected += ResetBot;

            var services = ConfigureServices();
            services.GetRequiredService<LogService>();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);
           
            if(!(isSharpTalkPresent = File.Exists("SharpTalkGenerator.exe")))
            {
                Console.WriteLine("SharpTalkGenerator is missing. The executable file for SharpTalk generator should be located in the same folder as StarBot. TTS functionality will be unavalible.");
                Console.WriteLine("Download SharpTalkGenerator at: https://github.com/dsdude123/SharpTalkGenerator/releases/latest");
            }

            try
            {
                await _client.LoginAsync(TokenType.Bot, _config["token"]);
            }
            catch (HttpRequestException e)
            {
                ResetBot(e);
            }

            UpdateYouTubeDl();

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task ResetBot(Exception ex)
        {
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
                .AddSingleton<Rule34Service>()
                .AddSingleton<XmlDocument>()
                .AddSingleton<UrbanDictionaryService>()
                .AddSingleton<UrbanClient>()
                .AddSingleton<WikipediaService>()
                .AddSingleton<MediawikiSharp_API.Mediawiki>()
                .AddSingleton<YouTubeService>()
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
                RedirectStandardOutput = false,
            };

            process.Start();
            process.WaitForExit();
        }
    }
}