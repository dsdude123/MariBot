using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Text
{
    public class OpenAIModule : ModuleBase<SocketCommandContext>
    {
        public Services.OpenAIService OpenAiService { get; set; }
        private DiscordSocketClient discord;
        private List<string> acceptedReplyIds;

        public OpenAIModule(DiscordSocketClient discordClient)
        {
            acceptedReplyIds = new List<string>();
            this.discord = discordClient;
            discord.MessageReceived += DiscordClient_MessageReceived;
        }

        [Command("gpt3", RunMode = RunMode.Async)]
        public async Task OpenAiTextCompletion([Remainder] string input)
        {
            try
            {
                string result = await OpenAiService.ExecuteGpt3Query(input);
                await Context.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id));
                return;
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
                return;
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        [Command("chatgpt", RunMode = RunMode.Async)]
        public async Task StartChatGptSession([Remainder] string input)
        {
            try
            {
                string result = await OpenAiService.ExecuteChatGptQuery(Context.Guild.Id, Context.Channel.Id, Context.Message.Id, input);
                RestUserMessage sentMessage = Context.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id)).Result;
                OpenAiService.UpdateId(Context.Guild.Id, Context.Channel.Id, Context.Message.Id, sentMessage.Id);
                return;
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
                return;
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        private async Task DiscordClient_MessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var replyContext = new SocketCommandContext(discord, message);
            if (replyContext.Message.Type == MessageType.Reply && IsAlreadyWorking(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id)) return;
            if (replyContext.Message.Type == MessageType.Reply && OpenAiService.CheckIfChatGpt(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id))
            {
                try
                {
                    string result = await OpenAiService.ExecuteChatGptQuery(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id, replyContext.Message.Content);
                    RestUserMessage sentMessage = replyContext.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(replyContext.Message.Id)).Result;
                    OpenAiService.UpdateId(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id, sentMessage.Id);
                }
                catch (ArgumentException)
                {
                    await replyContext.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(replyContext.Message.Id));
                    
                }
                catch (ApplicationException ex)
                {
                    await replyContext.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(replyContext.Message.Id));
                }
                ClearWorkingFlag(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id);
            }
            return;
        }

        private bool IsAlreadyWorking(ulong guildId, ulong channelId, ulong messageId)
        {
            var id = $"{guildId}-{channelId}-{messageId}";
            if (acceptedReplyIds.Contains(id))
            {
                return true;
            } else
            {
                acceptedReplyIds.Add(id);
                return false;
            }
        }

        private void ClearWorkingFlag(ulong guildId, ulong channelId, ulong messageId)
        {
            var id = $"{guildId}-{channelId}-{messageId}";
            acceptedReplyIds.Remove(id);
        }
    }
}
