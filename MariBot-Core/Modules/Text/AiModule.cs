using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MariBot.Core.Services;
using RestSharp.Extensions;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Module for commands related to cloud AI services.
    /// </summary>
    public class AiModule : ModuleBase<SocketCommandContext>
    {
        private readonly DataService dataService;
        private readonly OpenAiService openAiService;
        private readonly ImageService imageService;
        private readonly FluxService fluxService;
        private readonly ILogger<AiModule> logger;

        public AiModule(OpenAiService openAIService, DataService dataService, ImageService imageService, FluxService fluxService, ILogger<AiModule> logger)
        {
            this.openAiService = openAIService;
            this.dataService = dataService;
            this.imageService = imageService;
            this.fluxService = fluxService;
            this.logger = logger;
        }

        ///// <summary>
        ///// ChatGPT prompt completion command
        ///// </summary>
        ///// <param name="input">Input prompt</param>
        ///// <returns>Completed task</returns>
        //[Command("chatgpt", RunMode = RunMode.Async)]
        //public async Task StartChatGptSession([Remainder] string input)
        //{
        //    try
        //    {
        //        var result = await openAiService.ExecuteChatGptQuery(Context.Guild.Id, Context.Channel.Id, Context.Message.Id, input, Context.User.Id.ToString());
        //        var sentMessage = Context.Channel.SendMessageAsync($"```\n{result.Replace("```","")}\n```", messageReference: new MessageReference(Context.Message.Id)).Result;
        //        if (!dataService.UpdateChatGptMessageHistoryId(Context.Guild.Id, Context.Channel.Id, Context.Message.Id,
        //                sentMessage.Id))
        //        {
        //            throw new ApplicationException(
        //                "Failed to save message history to DB. ChatGPT context will be lost.");
        //        };
        //    }
        //    catch (ArgumentException)
        //    {
        //        await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
        //    }
        //    catch (ApplicationException ex)
        //    {
        //        await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
        //    }
        //}

        /// <summary>
        /// DALLE Image Command
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <returns></returns>
        [Command("dalle", RunMode = RunMode.Async)]
        [Alias("dallehd")]
        public async Task Dalle([Remainder] string prompt)
        {
            try
            {
                var imageUrl = await openAiService.ExecuteGptQuery(prompt, Context.User.Id.ToString(), Models.OpenAiModel.DALLE);
                var stream = await imageService.GetWebResource(imageUrl);
                await Context.Channel.SendFileAsync(stream, "dalle.png", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException.GetType() == typeof(ApplicationException))
                {
                    await Context.Channel.SendMessageAsync($"{ex.InnerException.Message}", messageReference: new MessageReference(Context.Message.Id));
                }
                else
                {
                    throw ex.InnerException;
                }
            }
        }

        /// <summary>
        /// GPT Image Image Command
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <returns></returns>
        [Command("gptimage", RunMode = RunMode.Async)]
        public async Task GptImage([Remainder] string prompt)
        {
            try
            {
                var imageUrl = await openAiService.ExecuteGptQuery(prompt, Context.User.Id.ToString(), Models.OpenAiModel.GPTIMAGE);
                var stream = await imageService.GetWebResource(imageUrl);
                await Context.Channel.SendFileAsync(stream, "dalle.png", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException.GetType() == typeof(ApplicationException))
                {
                    await Context.Channel.SendMessageAsync($"{ex.InnerException.Message}", messageReference: new MessageReference(Context.Message.Id));
                }
                else
                {
                    throw ex.InnerException;
                }
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
                var result = await openAiService.ExecuteGptQuery(input, Context.User.Id.ToString(), Models.OpenAiModel.GPT3);
                await Context.Channel.SendMessageAsync($"```\n{result.Replace("```", "")}\n```", messageReference: new MessageReference(Context.Message.Id));
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
                var result = await openAiService.ExecuteGptQuery(input, Context.User.Id.ToString(), Models.OpenAiModel.GPT4);
                await Context.Channel.SendMessageAsync($"```\n{result.Replace("```", "")}\n```", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// GPT-5 prompt completion command
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns></returns>
        [Command("gpt5", RunMode = RunMode.Async)]
        [Alias("gpt")]
        public async Task Gpt5TextCompletion([Remainder] string input)
        {
            try
            {
                var result = await openAiService.ExecuteGptQuery(input, Context.User.Id.ToString(), Models.OpenAiModel.GPT5);
                await Context.Channel.SendMessageAsync($"```\n{result.Replace("```", "")}\n```", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        /// <summary>
        /// Black Forest Labs Flux Image Generation
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns></returns>
        [Command("flux", RunMode= RunMode.Async)]
        public async Task FluxImageGeneration([Remainder] string input)
        {
            try
            {
                var result = await fluxService.GenerateFlux(input);
                var eb = new EmbedBuilder();
                eb.WithDescription(result.prompt);
                var stream = await imageService.GetWebResource(result.sample);
                var filename = $"{Guid.NewGuid().ToString()}.jpeg";
                eb.WithImageUrl($"attachment://{filename}");
                await File.WriteAllBytesAsync(filename, stream.ReadAsBytes());
                await Context.Channel.SendFileAsync(filename, embed: eb.Build(), messageReference: new MessageReference(Context.Message.Id));
                File.Delete(filename);
            } catch (Exception ex) {
                await Context.Channel.SendMessageAsync($"Something went really wrong. {ex.Message}", messageReference: new MessageReference(Context.Message.Id));
                logger.LogCritical(ex, "Unhandled service exception");
            }
        }
    }
}
