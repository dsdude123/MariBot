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

        [Command("uwu", RunMode = RunMode.Async)]
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

        [Command("spongebob", RunMode = RunMode.Async)]
        public async Task Spongbobify([Remainder] string input = null)
        {
            if (input == null)
            {
                input = (await Context.Channel.GetMessagesAsync(2, Discord.CacheMode.AllowDownload)
                        .FlattenAsync())
                    .ToArray()
                    .OrderBy(message => message.Timestamp)
                    .First().Content;
            }
            var result = MockSpongebobText(input);

            var eb = new EmbedBuilder();
            eb.WithDescription(result);
            eb.WithImageUrl("http://nerv.jpn.com/discord/mocking-spongebob.jpg");

            await Context.Channel.SendMessageAsync(embed: eb.Build());
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

        private string MockSpongebobText(string input)
        {
            var result = "";

            var lastCapped = new Random().Next() > (int.MaxValue / 2);

            foreach (var character in input.ToCharArray())
            {
                if (char.IsLetter(character) && lastCapped)
                {
                    result += character.ToString().ToLower();
                    lastCapped = false;
                }
                else if (char.IsLetter(character) && !lastCapped)
                {
                    result += character.ToString().ToUpper();
                    lastCapped = true;
                }
                else
                {
                    result += character;
                }
            }

            return result;
        }
    }
}
