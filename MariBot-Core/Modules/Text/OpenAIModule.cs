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

        public OpenAiModule(OpenAiService openAIService, DataService dataService)
        {
            this.openAiService = openAIService;
            this.dataService = dataService;
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
                var sentMessage = Context.Channel.SendMessageAsync($"```\n{result.Replace("```","")}\n```", messageReference: new MessageReference(Context.Message.Id)).Result;
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
                await Context.Channel.SendMessageAsync($"```\n{result.Replace("```", "")}\n```", messageReference: new MessageReference(Context.Message.Id));
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
                await Context.Channel.SendMessageAsync($"```\n{result.Replace("```", "")}\n```", messageReference: new MessageReference(Context.Message.Id));
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
    }
}
