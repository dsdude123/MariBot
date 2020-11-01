using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MariBot.Services;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private StaticTextResponseService staticTextResponseService = new StaticTextResponseService();

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            if (!(message.HasMentionPrefix(_discord.CurrentUser, ref argPos) || message.HasStringPrefix(Program._config["prefix"] + " ",ref argPos))) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue && 
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString());

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
            {
                string[] parts = message.Content.Split(' ');
                if (parts.Length >= 2) {
                    string textResponseLookup = parts[1];
                    Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
                    if(userIdCheck.IsMatch(textResponseLookup))
                    {
                        textResponseLookup = textResponseLookup.Replace("!", string.Empty);
                    }
                    try
                    {
                        string response = StaticTextResponseService.getGlobalResponse(textResponseLookup);
                        await context.Channel.SendMessageAsync(response);
                        return;
                    } catch (KeyNotFoundException ex)
                    {
                        // Do nothing
                    }
                    try
                    {
                        string response = staticTextResponseService.getResponse(context.Guild.Id,textResponseLookup);
                        await context.Channel.SendMessageAsync(response);
                        return;
                    }
                    catch (KeyNotFoundException ex)
                    {
                        // Do nothing
                    }
                }
            }
        }
    }
}
