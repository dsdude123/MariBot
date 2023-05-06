using Discord;
using Discord.Commands;
using MariBot.Core.Models.ChatGPT;
using OpenAI.GPT3;
using OpenAI.GPT3.ObjectModels.RequestModels;
using MessageType = MariBot.Core.Models.ChatGPT.MessageType;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing interface to OpenAI APIs such as DALLE, GPT-3, GPT-4, and ChatGPT
    /// </summary>
    public class OpenAiService
    {
        private readonly OpenAI.GPT3.Managers.OpenAIService apiClient;
        private readonly DataService dataService;
        private readonly ILogger<OpenAiService> logger;

        public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger, DataService dataService)
        {
            this.logger = logger;
            this.dataService = dataService;
            apiClient = new OpenAI.GPT3.Managers.OpenAIService(
                new OpenAiOptions()
                {
                    ApiKey = configuration["DiscordSettings:OpenAiApiKey"]
                });
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
        /// Generates DALLE image
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns>Url to the image</returns>
        /// <exception cref="ArgumentException">Input fails safety checks</exception>
        /// <exception cref="ApplicationException">API error</exception>
        public async Task<string> ExecuteDalleQuery(string input)
        {
            // Perform safety checks
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            if (moderationResult.Results.Any(moderation => moderation.Flagged))
            {
                throw new ArgumentException("Message failed safety checks.");
            }

            // Call OpenAI
            var imageResult = await apiClient.CreateImage(new ImageCreateRequest()
            {
                Prompt = input,
                Size = "1024x1024",
                N = 1 // Generate N images
            });

            // Return result
            if (imageResult.Successful)
            {
                return imageResult.Results.First().Url;
            }
            else
            {
                throw new ApplicationException($"{imageResult.Error.Code}: {imageResult.Error.Message}");
            }
        }

        /// <summary>
        /// Generates text response using GPT-3
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns>Response text</returns>
        /// <exception cref="ArgumentException">Input fails safety checks</exception>
        /// <exception cref="ApplicationException">API error</exception>
        public async Task<string> ExecuteGpt3Query(string input)
        {
            // Perform safety checks
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            if (moderationResult.Results.Any(moderation => moderation.Flagged))
            {
                throw new ArgumentException("Message failed safety checks.");
            }

            // Call OpenAI
            var textResult = await apiClient.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = input,
                MaxTokens = 500
            }, OpenAI.GPT3.ObjectModels.Models.TextDavinciV3);

            // Return result
            if (textResult.Successful)
            {
                var text = textResult.Choices.FirstOrDefault().Text;

                // Trim to meet Discord message length limits
                if (text.Length > 1992)
                {
                    text = text[..1992];
                }

                return text;
            }
            else
            {
                throw new ApplicationException($"{textResult.Error.Code}: {textResult.Error.Message}");
            }
        }

        /// <summary>
        /// Generates text response using GPT-4
        /// </summary>
        /// <param name="input">Input prompt</param>
        /// <returns>Response text</returns>
        /// <exception cref="ArgumentException">Input fails safety checks</exception>
        /// <exception cref="ApplicationException">API error</exception>
        public async Task<string> ExecuteGpt4Query(string input)
        {
            // Perform safety checks
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            if (moderationResult.Results.Any(moderation => moderation.Flagged))
            {
                throw new ArgumentException("Message failed safety checks.");
            }

            // Call OpenAI
            var completionResult = await apiClient.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = new[] {new ChatMessage("user", input)},
                MaxTokens = 500

            }, OpenAI.GPT3.ObjectModels.Models.Gpt4); // TODO: Upgrade to 32k when available

            // Return result
            if (completionResult.Successful)
            {
                var text = completionResult.Choices.FirstOrDefault().Message.Content;

                // Trim to meet Discord message length limits
                if (text.Length > 1992)
                {
                    text = text[..1992];
                }

                return text;
            }
            else
            {
                throw new ApplicationException($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
        }

        /// <summary>
        /// Generates text response using ChatGPT
        /// </summary>
        /// <param name="guildId">Guild ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="messageId">Message ID</param>
        /// <param name="input">New message</param>
        /// <returns>Response message</returns>
        /// <exception cref="NotImplementedException">Message type not supported</exception>
        /// <exception cref="ArgumentException">Input failed safety checks</exception>
        /// <exception cref="ApplicationException">API error</exception>
        public async Task<string> ExecuteChatGptQuery(ulong guildId, ulong channelId, ulong messageId, string input)
        {

            // Perform safety checks
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            if (moderationResult.Results.Any(moderation => moderation.Flagged))
            {
                throw new ArgumentException("Message failed safety checks.");
            }

            // Get message history
            var history = dataService.GetChatGptMessageHistory(guildId, channelId, messageId) ?? new MessageHistory
            {
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                Messages = new List<Tuple<MessageType, string>>()
            };

            history.Messages.Add(new Tuple<MessageType, string>(MessageType.User, input));

            var messages = new List<ChatMessage>();

            foreach (var message in history.Messages)
            {
                switch (message.Item1)
                {
                    case MessageType.Assistant:
                        messages.Add(ChatMessage.FromAssistant(message.Item2));
                        break;
                    case MessageType.User:
                        messages.Add(ChatMessage.FromUser(message.Item2));
                        break;
                    default:
                        throw new NotImplementedException("MessageType not supported.");
                }
            }

            // Call OpenAI
            var completionResult = await apiClient.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                MaxTokens = 500,
                Model = OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo
            });

            // Save new message history and return result
            if (completionResult.Successful)
            {
                var text = completionResult.Choices.FirstOrDefault().Message.Content;

                // Trim to meet Discord message length limits
                if (text.Length > 1992)
                {
                    text = text[..1992];
                }

                history.Messages.Add(new Tuple<MessageType, string>(MessageType.Assistant, text));
                dataService.UpdateChatGptMessageHistory(history);

                return text;
            }
            else
            {
                throw new ApplicationException($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
        }

        /// <summary>
        /// Handler for replies to ChatGPT responses
        /// </summary>
        /// <param name="arg">Discord socket context</param>
        /// <returns>Completed task</returns>
        public async Task HandleReply(SocketCommandContext replyContext)
        {
            if (replyContext.Message.Type == Discord.MessageType.Reply && CheckIfChatGpt(replyContext.Guild.Id,
                    replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id))
            {
                try
                {
                    var result = await ExecuteChatGptQuery(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id, replyContext.Message.Content);
                    var sentMessage = replyContext.Channel.SendMessageAsync($"```\n{result}\n```", messageReference: new MessageReference(replyContext.Message.Id)).Result;
                    if (!dataService.UpdateChatGptMessageHistoryId(replyContext.Guild.Id, replyContext.Channel.Id, replyContext.Message.ReferencedMessage.Id,
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
            }
        }
    }
}
