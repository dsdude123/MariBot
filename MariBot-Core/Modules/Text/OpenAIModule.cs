using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Module for commands related to OpenAI services.
    /// </summary>
    public class OpenAiModule : ModuleBase<SocketCommandContext>
    {
        private readonly DataService dataService;
        private readonly OpenAiService openAiService;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private List<string> acceptedReplyIds;
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private DiscordSocketClient discord;


        public OpenAiModule(OpenAiService openAIService, DiscordSocketClient discord, DataService dataService)
        {
            this.openAiService = openAIService;
            this.discord = discord;
            this.dataService = dataService;

            acceptedReplyIds = new List<string>();
            discord.MessageReceived += MessageReceived;
        }

        /// <summary>
        /// ChatGPT prompt completion command
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns>Completed task</returns>
        [Command("chatgpt", RunMode = RunMode.Async)]
        public async Task StartChatGptSession([Remainder] string input)
        {
            try
            {
                var result = await openAiService.ExecuteChatGptQuery(Context.Guild.Id, Context.Channel.Id, Context.Message.Id, input);
                var sentMessage = Context.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id)).Result;
                if (!dataService.UpdateChatGptMessageHistoryId(Context.Guild.Id, Context.Channel.Id, Context.Message.Id,
                        sentMessage.Id))
                {
                    throw new ApplicationException(
                        "Failed to save message history to DB. ChatGPT context will be lost.");
                };
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// DALLE Image Command
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <returns></returns>
        [RequireOwner]
        [Command("dalle", RunMode = RunMode.Async)]
        public async Task Dalle([Remainder] string prompt)
        {
            try
            {
                var imageUrl = openAiService.ExecuteDalleQuery(prompt).Result;
                await Context.Channel.SendMessageAsync($"{imageUrl}", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// GPT-3 prompt completion command
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns></returns>
        [Command("gpt3", RunMode = RunMode.Async)]
        public async Task Gpt3TextCompletion([Remainder] string input)
        {
            try
            {
                var result = await openAiService.ExecuteGpt3Query(input);
                await Context.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// GPT-4 prompt completion command
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns></returns>
        [Command("gpt4", RunMode = RunMode.Async)]
        public async Task Gpt4TextCompletion([Remainder] string input)
        {
            try
            {
                var result = await openAiService.ExecuteGpt4Query(input);
                await Context.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// Message handler for replies to ChatGPT responses
        /// </summary>
        /// <param name="arg">Discord socket message</param>
        /// <returns>Completed task</returns>
        private async Task MessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var replyContext = new SocketCommandContext(discord, message);
            switch (replyContext.Message.Type)
            {
                case MessageType.Reply when IsAlreadyWorking(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id):
                    return;
                case MessageType.Reply when openAiService.CheckIfChatGpt(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id):
                    try
                    {
                        var result = await openAiService.ExecuteChatGptQuery(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id, replyContext.Message.Content);
                        var sentMessage = replyContext.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(replyContext.Message.Id)).Result;
                        if (!dataService.UpdateChatGptMessageHistoryId(Context.Guild.Id, Context.Channel.Id, Context.Message.Id,
                                sentMessage.Id))
                        {
                            throw new ApplicationException(
                                "Failed to save message history to DB. ChatGPT context will be lost.");
                        };
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
                    break;
            }

            return;
        }

        private bool IsAlreadyWorking(ulong guildId, ulong channelId, ulong messageId)
        {
            var id = $"{guildId}-{channelId}-{messageId}";
            if (acceptedReplyIds.Contains(id))
            {
                return true;
            }
            else
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
