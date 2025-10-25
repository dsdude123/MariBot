using Discord;
using Discord.Commands;
using MariBot.Core.Models;
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
        private readonly ChatClient gpt3Client;
        private readonly ChatClient gpt4Client;
        private readonly ChatClient gpt5Client;
        private readonly ModerationClient moderationClient;
        private readonly ImageClient dalleClient;
        private readonly ImageClient gptImageClient;
        private readonly DataService dataService;
        private readonly ILogger<OpenAiService> logger;

        public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger, DataService dataService)
        {
            this.logger = logger;
            this.dataService = dataService;
            var apiKey = configuration["DiscordSettings:OpenAiApiKey"];
            var orgId = configuration["DiscordSettings:OpenAiOrganization"];

            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);
            OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions
            {
                OrganizationId = orgId
            };

            gpt3Client = new ChatClient("gpt-3.5-turbo-1106", apiKeyCredential, openAIClientOptions);
            gpt4Client = new ChatClient("gpt-4.1", apiKeyCredential, openAIClientOptions);
            gpt5Client = new ChatClient("gpt-5", apiKeyCredential, openAIClientOptions);

            moderationClient = new ModerationClient("omni-moderation-latest", apiKeyCredential, openAIClientOptions);

            dalleClient = new ImageClient("dall-e-3", apiKeyCredential, openAIClientOptions);
            gptImageClient = new ImageClient("gpt-image-1", apiKeyCredential, openAIClientOptions);
        }

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
        public async Task<string> ExecuteGptQuery(string input, string userid, OpenAiModel model)
        {
            switch (model)
            {
                case OpenAiModel.GPT3:
                    return await ExecuteGenericGptQuery(gpt3Client, input, userid);
                case OpenAiModel.GPT4:
                    return await ExecuteGenericGptQuery(gpt4Client, input, userid);
                case OpenAiModel.GPT5:
                    return await ExecuteGenericGptQuery(gpt5Client, input, userid);
                case OpenAiModel.DALLE:
                    return await ExecuteGenericImage(dalleClient, input, userid);
                case OpenAiModel.GPTIMAGE:
                    return await ExecuteGenericImage(gptImageClient, input, userid);
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
                var moderationResult = moderationClient.ClassifyText(input);

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

                // Trim to meet Discord message length limits
                if (gptContent.Length > 1992)
                {
                    gptContent = gptContent[..1992];
                }

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
                var moderationResult = await moderationClient.ClassifyTextAsync(input);

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
