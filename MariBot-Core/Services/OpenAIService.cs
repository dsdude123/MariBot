using Discord;
using Discord.Commands;
using MariBot.Core.Models;
using MariBot.Core.Models.Config;
using MariBot.Core.Models.ChatGPT;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Moderations;
using System.ClientModel;
using ApplicationException = System.ApplicationException;
using MessageType = MariBot.Core.Models.ChatGPT.MessageType;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing interface to OpenAI APIs such as DALLE, GPT-3, GPT-4, and ChatGPT
    /// </summary>
    public class OpenAiService
    {
        // Null when no usable API key was configured. ApiKeyCredential rejects a
        // null or empty key in its constructor, and Discord.Net resolves this
        // service while building its command list at startup, so an unset key
        // used to stop the whole bot rather than the commands that need it.
        // Copying .env.example without filling it in is enough to hit that: it
        // ships the key blank, and compose passes a blank through as an empty
        // string rather than leaving it unset.
        private readonly ChatClient? gpt4Client;
        private readonly ChatClient? gpt5Client;
        private readonly ModerationClient? moderationClient;
        private readonly ImageClient? gptImageClient;
        private readonly DataService dataService;
        private readonly ILogger<OpenAiService> logger;

        /// <summary>For mocking in tests, matching DataService's seam.</summary>
        protected OpenAiService() { logger = null!; dataService = null!; }

        public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger, DataService dataService)
        {
            this.logger = logger;
            this.dataService = dataService;
            var apiKey = configuration["DiscordSettings:OpenAiApiKey"];
            var orgId = configuration["DiscordSettings:OpenAiOrganization"];

            if (ConfiguredValue.IsUnset(apiKey))
            {
                logger.LogWarning(
                    "DiscordSettings:OpenAiApiKey is not set. OpenAI commands are disabled.");
                return;
            }

            try
            {
                ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);
                OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions
                {
                    // The placeholder is not an organization, and sending it as
                    // one turns a working key into a confusing 401.
                    OrganizationId = ConfiguredValue.IsUnset(orgId) ? null : orgId
                };

                // Built into locals and assigned together, so a throw part way
                // through cannot leave this half-configured.
                //
                // Model ids are pinned here rather than left on a moving alias so
                // that an upgrade is a reviewable change with a date on it. See
                // https://developers.openai.com/api/docs/deprecations for what is
                // scheduled to stop answering and when.
                var gpt4 = new ChatClient("gpt-4.1", apiKeyCredential, openAIClientOptions);
                var gpt5 = new ChatClient("gpt-5.6-terra", apiKeyCredential, openAIClientOptions);
                var moderation = new ModerationClient("omni-moderation-latest", apiKeyCredential, openAIClientOptions);
                var gptImage = new ImageClient("gpt-image-2", apiKeyCredential, openAIClientOptions);

                gpt4Client = gpt4;
                gpt5Client = gpt5;
                moderationClient = moderation;
                gptImageClient = gptImage;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "DiscordSettings:OpenAiApiKey was rejected: {Message}. OpenAI commands are disabled.",
                    ex.Message);
            }
        }

        /// <summary>
        /// Whether a usable API key was configured. False means every OpenAI
        /// command will refuse rather than call.
        /// </summary>
        public bool IsConfigured => gpt4Client != null;

        private T Require<T>(T? client) where T : class =>
            client ?? throw new InvalidOperationException(
                "OpenAI is not configured. Set DiscordSettings:OpenAiApiKey.");

        /// <summary>
        /// Checks if a Chat GPT message history exists
        /// </summary>
        /// <param name="guildId">Guild ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="messageId">Message ID</param>
        /// <returns>True if history exists</returns>
        public bool CheckIfChatGpt(ulong guildId, ulong channelId, ulong messageId)
        {
            return dataService.GetChatGptMessageHistory(guildId, channelId, messageId) != null;
        }

        /// <summary>
        /// Generates response using selected GPT model
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns>Response text</returns>
        /// <exception cref="ArgumentException">Input fails safety checks</exception>
        /// <exception cref="ApplicationException">API error</exception>
        public virtual async Task<string> ExecuteGptQuery(string input, string userid, OpenAiModel model)
        {
            switch (model)
            {
                case OpenAiModel.GPT4:
                    return await ExecuteGenericGptQuery(Require(gpt4Client), input, userid);
                case OpenAiModel.GPT5:
                    return await ExecuteGenericGptQuery(Require(gpt5Client), input, userid);
                case OpenAiModel.GPTIMAGE:
                    return await ExecuteGenericImage(Require(gptImageClient), input, userid);
                default:
                    throw new ArgumentOutOfRangeException(nameof(model));
            }
        }

        ///// <summary>
        ///// Generates text response using ChatGPT
        ///// </summary>
        ///// <param name="guildId">Guild ID</param>
        ///// <param name="channelId">Channel ID</param>
        ///// <param name="messageId">Message ID</param>
        ///// <param name="input">New message</param>
        ///// <returns>Response message</returns>
        ///// <exception cref="NotImplementedException">Message type not supported</exception>
        ///// <exception cref="ArgumentException">Input failed safety checks</exception>
        ///// <exception cref="ApplicationException">API error</exception>
        //public async Task<string> ExecuteChatGptQuery(ulong guildId, ulong channelId, ulong messageId, string input, string userId)
        //{

        //    // Perform safety checks
        //    var moderationResult = await apiClient.Moderation.CreateModeration(new CreateModerationRequest()
        //    {
        //        Input = input,
        //        Model = "omni-moderation"
        //    });

        //    if (moderationResult.Results.Any(moderation => moderation.Flagged))
        //    {
        //        throw new ArgumentException("Message failed safety checks.");
        //    }

        //    // Get message history
        //    var history = dataService.GetChatGptMessageHistory(guildId, channelId, messageId) ?? new MessageHistory
        //    {
        //        GuildId = guildId,
        //        ChannelId = channelId,
        //        MessageId = messageId,
        //        Messages = new List<Tuple<MessageType, string>>()
        //    };

        //    history.Messages.Add(new Tuple<MessageType, string>(MessageType.User, input));

        //    var messages = new List<ChatMessage>();

        //    foreach (var message in history.Messages)
        //    {
        //        switch (message.Item1)
        //        {
        //            case MessageType.Assistant:
        //                messages.Add(ChatMessage.FromAssistant(message.Item2));
        //                break;
        //            case MessageType.User:
        //                messages.Add(ChatMessage.FromUser(message.Item2));
        //                break;
        //            default:
        //                throw new NotImplementedException("MessageType not supported.");
        //        }
        //    }

        //    // Call OpenAI
        //    var completionResult = await apiClient.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        //    {
        //        Messages = messages,
        //        MaxTokens = 500,
        //        Model = "gpt-5",
        //        User = userId
        //    });

        //    // Save new message history and return result
        //    if (completionResult.Successful)
        //    {
        //        var text = completionResult.Choices.FirstOrDefault().Message.Content;

        //        // Trim to meet Discord message length limits
        //        if (text.Length > 1992)
        //        {
        //            text = text[..1992];
        //        }

        //        history.Messages.Add(new Tuple<MessageType, string>(MessageType.Assistant, text));
        //        dataService.UpdateChatGptMessageHistory(history);

        //        return text;
        //    }
        //    else
        //    {
        //        throw new ApplicationException($"{completionResult.Error.Code}: {completionResult.Error.Message}");
        //    }
        //}

        ///// <summary>
        ///// Handler for replies to ChatGPT responses
        ///// </summary>
        ///// <param name="arg">Discord socket context</param>
        ///// <returns>Completed task</returns>
        //public async Task HandleReply(SocketCommandContext replyContext)
        //{
        //    if (replyContext.Message.Type == Discord.MessageType.Reply && CheckIfChatGpt(replyContext.Guild.Id,
        //            replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id))
        //    {
        //        try
        //        {
        //            var result = await ExecuteChatGptQuery(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id, replyContext.Message.Content, replyContext.User.Id.ToString());
        //            var sentMessage = replyContext.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(replyContext.Message.Id)).Result;
        //            if (!dataService.UpdateChatGptMessageHistoryId(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id,
        //                    sentMessage.Id))
        //            {
        //                throw new ApplicationException(
        //                    "Failed to save message history to DB. ChatGPT context will be lost.");
        //            };
        //        }
        //        catch (ArgumentException)
        //        {
        //            await replyContext.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(replyContext.Message.Id));

        //        }
        //        catch (ApplicationException ex)
        //        {
        //            await replyContext.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(replyContext.Message.Id));
        //        }
        //    }
        //}

        private async Task<string> ExecuteGenericGptQuery(ChatClient chatClient, string input, string userid)
        {
            try
            {
                var moderationResult = Require(moderationClient).ClassifyText(input);

                if (moderationResult.Value.Flagged)
                {
                    return "Input failed safety checks.";
                }

                ChatCompletionOptions chatCompletionOptions = new ChatCompletionOptions
                {
                    EndUserId = userid
                };

                UserChatMessage userChatMessage = new UserChatMessage(input);
                List<ChatMessage> messages = new();
                messages.Add(userChatMessage);

                var gptResult = await chatClient.CompleteChatAsync(messages, chatCompletionOptions);
                var gptContent = gptResult.Value.Content.First().Text;

                return gptContent;
            }
            catch (Exception ex)
            {
                logger.LogError("Exception in OpenAI generic text generation", ex);
                return ex.Message;
            }
        }

        private async Task<string> ExecuteGenericImage(ImageClient imageClient, string input, string userid)
        {
            try
            {
                var moderationResult = await Require(moderationClient).ClassifyTextAsync(input);

                if (moderationResult.Value.Flagged)
                {
                    return "Input failed safety checks.";
                }

                ImageGenerationOptions options = new ImageGenerationOptions
                {
                    EndUserId = userid,
                    Quality = GeneratedImageQuality.High,
                    Size = GeneratedImageSize.W1024xH1024
                };

                var imageResult = await imageClient.GenerateImageAsync(input, options);
                return imageResult.Value.ImageUri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                logger.LogError("Exception in OpenAI generic image generation", ex);
                return ex.Message;
            }
        }
    }
}
