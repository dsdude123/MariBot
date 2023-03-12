using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace MariBot_Core.Services
{
    public class DiscordBotService : IHostedService
    {
        private DiscordSocketClient client;
        private readonly IConfiguration configuration;
        private ILogger<DiscordBotService> logger;
        private InteractionService interactionService;
        private CommandHandlingService commandHandlingService;

        public DiscordBotService(DiscordSocketClient client, IConfiguration configuration, ILogger<DiscordBotService> logger, InteractionService interactionService, CommandHandlingService commandHandlingService)
        {
            this.client = client;
            this.configuration = configuration;
            this.logger = logger;
            this.interactionService = interactionService;
            this.commandHandlingService = commandHandlingService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting up Discord bot");
            client.Log += Log;
            var botToken = configuration.GetValue<string>("DiscordSettings:BotToken");

            // Setup command handling
            client.Ready += async () =>
            {
                await interactionService.RegisterCommandsGloballyAsync(true);
            };
            await commandHandlingService.InitializeAsync();

            await client.LoginAsync(TokenType.Bot, botToken);
            client.StartAsync();
        }

        public ConnectionState GetClientStatus()
        {
            return client.ConnectionState;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                client.Dispose();
            }
            catch
            {
                logger.LogWarning("Failed to dispose Discord client.");
            }
        }

        private Task Log(LogMessage arg)
        {
            var message = arg.Message;
            if (message == null && arg.Exception != null)
            {
                message = arg.Exception.Message;
            }
            switch(arg.Severity)
            {
                case LogSeverity.Critical:
                    logger.LogCritical("{} - {}", arg.Source, message);
                    break;
                case LogSeverity.Error:
                    logger.LogError("{} - {}", arg.Source, message);
                    break;
                case LogSeverity.Warning:
                    logger.LogWarning("{} - {}", arg.Source, message);
                    break;
                case LogSeverity.Info:
                    logger.LogInformation("{} - {}", arg.Source, message);
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    logger.LogTrace("{} - {}", arg.Source, message);
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
