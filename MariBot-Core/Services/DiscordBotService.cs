using Discord;
using Discord.WebSocket;

namespace MariBot_Core.Services
{
    public class DiscordBotService : IHostedService
    {
        private DiscordSocketClient client;
        private readonly IConfiguration configuration;
        private ILogger<DiscordBotService> logger;

        public DiscordBotService(DiscordSocketClient client, IConfiguration configuration, ILogger<DiscordBotService> logger)
        {
            this.client = client;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting up Discord bot");
            client.Log += Log;
            var botToken = configuration.GetValue<string>("DiscordSettings:BotToken");
            logger.LogInformation("Got token {}", botToken);
            await client.LoginAsync(TokenType.Bot, botToken);
            client.StartAsync();
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
