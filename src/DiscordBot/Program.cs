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
using MariBot.Services;
using System.Threading;
using Discord.Interactions;
using LiteDB;
using Microsoft.Extensions.Logging;
using DiscordBot.Services;

namespace MariBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static IConfiguration config;
        private LiteDatabase database;

        public async Task MainAsync()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            client = new DiscordSocketClient(clientConfig);
            config = BuildConfig();

            var services = ConfigureServices();
            var logService = services.GetRequiredService<LogService>();
            var interactionService = services.GetRequiredService<InteractionService>();
            database = new LiteDatabase("database.db");

            client.Ready += async () =>
            {
                await interactionService.RegisterCommandsToGuildAsync(297485054836342786, true);
            };

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await client.LoginAsync(TokenType.Bot, config["token"]);

            await client.StartAsync();

            await Task.Delay(-1);
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
                .AddSingleton<XmlDocument>()
                .AddSingleton(x => new SpookeningService(x.GetRequiredService<DiscordSocketClient>(), database))
                .BuildServiceProvider();
        }

        private IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }
    }
}