using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Commands for text manipulation actions
    /// </summary>
    public class TextModule : ModuleBase<SocketCommandContext>
    {
        private static readonly string Biohazard = "☣️";
        private static readonly Emoji BiohazardEmoji = new(Biohazard);

        private readonly DynamicConfigService dynamicConfigService;

        public TextModule(DiscordSocketClient discordClient, DynamicConfigService dynamicConfigService)
        {
            this.dynamicConfigService = dynamicConfigService;
            discordClient.ReactionAdded += ReactionAddedHandler;
        }

        [Command("uwu")]
        public async Task UwuifyAsync([Remainder] string input = null)
        {
            if (input == null)
            {
                input = (await Context.Channel.GetMessagesAsync(2, Discord.CacheMode.AllowDownload)
                        .FlattenAsync())
                    .ToArray()
                    .OrderBy(message => message.Timestamp)
                    .First().Content;
            }
            input = UwuifyText(input);

            await Context.Channel.SendMessageAsync(input);
        }

        private Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> userMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            Task.Run(async () =>
            {
                if (reaction.Emote.Name == Biohazard)
                {
                    var message = await userMessage.GetOrDownloadAsync();
                    var channelContext = await channel.GetOrDownloadAsync();
                    var socketGuildChannel = (SocketGuildChannel)channelContext;
                    if (dynamicConfigService.CheckFeatureEnabled(socketGuildChannel.Guild.Id, "emoji-triggers") && message.Reactions.TryGetValue(BiohazardEmoji, out var reactionMetadata) && !reactionMetadata.IsMe)
                    {
                        var text = UwuifyText(message.Content);
                        await channelContext.SendMessageAsync(text);
                        await message.AddReactionAsync(BiohazardEmoji);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private string UwuifyText(string input)
        {
            input = Regex.Replace(input, "(?:r|l)", "w");
            input = Regex.Replace(input, "(?:R|L)", "W");
            input = Regex.Replace(input, "n([aeiou])", "ny$1");
            input = Regex.Replace(input, "N([aeiou])", "Ny$1");
            input = Regex.Replace(input, "N([AEIOU])", "NY$1");
            input = Regex.Replace(input, "ove", "uv");
            return input;
        }
    }
}
