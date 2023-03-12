using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace MariBot_Core.Services
{
    public class CommandHandlingService
    {
        private ILogger<CommandHandlingService> logger;
        private IServiceProvider serviceProvider;
        private readonly DiscordSocketClient discord;
        private readonly CommandService commandService;
        private readonly InteractionService interactionService;
        private readonly IConfiguration configuration;

        public CommandHandlingService(ILogger<CommandHandlingService> logger, IServiceProvider serviceProvider, DiscordSocketClient discord, CommandService commandService, InteractionService interactionService, IConfiguration configuration)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.discord = discord;
            this.commandService = commandService;
            this.interactionService = interactionService;
            this.configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            logger.LogInformation("Starting command handler");
            discord.MessageReceived += MessageReceived;
            discord.IntegrationCreated += InteractionCreated;

            interactionService.SlashCommandExecuted += SlashCommandExecuted;
            interactionService.ContextCommandExecuted += ContextCommandExecuted;
            interactionService.ComponentCommandExecuted += ComponentCommandExecuted;

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task InteractionCreated(Discord.IIntegration arg)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(discord, message);

            var prefix = configuration.GetValue<string>("DiscordSettings:Prefix");

            //TODO: Get Guild Config
            //TODO: Add hooks for latex and file modifications

            //TODO: Emoji auto reaction

            int argPos = 0;
            string[] parts = message.Content.Split(' ');

            // Check if message has prefix to trigger the bot
            if (!(message.HasMentionPrefix(discord.CurrentUser, ref argPos) || message.HasStringPrefix(prefix + " ", ref argPos))) return;

            if (parts.Length < 2) return; // Make sure we actually have a command
            string requestedCommand = parts[1];
            logger.LogInformation("User \"{}\" requested command \"{}\" in \"{}/{}\"", context.User.ToString(), requestedCommand, context.Guild.ToString(), context.Channel.ToString());

            var result = await commandService.ExecuteAsync(context, argPos, serviceProvider);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ToString());
                return;
            }

            return;
        }
    }
}
