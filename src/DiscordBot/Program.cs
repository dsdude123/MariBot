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

namespace MariBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

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

            try
            {
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
                .AddSingleton<XmlDocument>()
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