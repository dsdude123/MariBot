using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using MariBot.Services;
using Newtonsoft.Json;

namespace MariBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commandService;
        private readonly InteractionService interactionService;
        private IServiceProvider provider;

        private Regex keepOnlyAlphaNum = new Regex("[^a-zA-Z0-9 -]");

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, InteractionService interactionService)
        {
            this.discord = discord;
            commandService = commands;
            this.provider = provider;
            this.interactionService = interactionService;
        }

        public async Task InitializeAsync()
        {

            discord.MessageReceived += MessageReceived;
            discord.InteractionCreated += InteractionCreated;

            interactionService.SlashCommandExecuted += SlashCommandExecuted;
            interactionService.ContextCommandExecuted += ContextCommandExecuted;
            interactionService.ComponentCommandExecuted += ComponentCommandExecuted;

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private async Task InteractionCreated(SocketInteraction interaction)
        {
            //try
            //{
                var context = new SocketInteractionContext(discord, interaction);
                await interactionService.ExecuteCommandAsync(context, provider);
            //}
            //catch (Exception ex)
            //{       
            //    if (interaction.Type == InteractionType.ApplicationCommand)
            //        await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            //}
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(discord, message);

            // Text Commands

            int argPos = 0;
            string[] parts = message.Content.Split(' ');
            if (!(message.HasMentionPrefix(discord.CurrentUser, ref argPos) || message.HasStringPrefix(Program.config["prefix"] + " ",ref argPos))) return;

            var result = await commandService.ExecuteAsync(context, argPos, provider);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ToString());
                return;
            }
        }
    }
}
