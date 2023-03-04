using MariBot.Models.ChatGPT;
using OpenAI.GPT3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace MariBot.Services
{
    public class OpenAIService
    {
        private static string globalPath = Environment.CurrentDirectory + "\\data\\global\\chatgpt.json";
        private static readonly string ApiKey = MariBot.Program.config["openAiApiKey"];
        private MessageStore messageStore;
        private OpenAI.GPT3.Managers.OpenAIService apiClient;

        public OpenAIService()
        {
            apiClient = new OpenAI.GPT3.Managers.OpenAIService(new OpenAiOptions() { ApiKey = ApiKey });

            if (File.Exists(globalPath))
            {
                messageStore = JsonConvert.DeserializeObject<MessageStore>(File.ReadAllText(globalPath));
            } else
            {
                messageStore = new MessageStore();
            }
        }

        public bool CheckIfChatGpt(ulong guildId, ulong channelId, ulong messageId)
        {
            return messageStore.KeyExists(guildId, channelId, messageId);
        }

        public async Task<string> ExecuteDalleQuery(string input)
        {
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            foreach (var moderation in moderationResult.Results)
            {
                if (moderation.Flagged)
                {
                    throw new ArgumentException("Message failed safety checks.");
                }
            }

            var imageResult = await apiClient.CreateImage(new ImageCreateRequest()
            {
                Prompt = input,
                Size = "1024x1024",
                N = 1
            });

            if (imageResult.Successful)
            {
                return imageResult.Results.First().Url;
            }
            else
            {
                throw new ApplicationException($"{imageResult.Error.Code}: {imageResult.Error.Message}");
            }
        }

        public async Task<string> ExecuteGpt3Query(string input)
        {
            var moderationResult = await apiClient.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            foreach (var moderation in moderationResult.Results)
            {
                if (moderation.Flagged)
                {
                    throw new ArgumentException("Message failed safety checks.");
                }
            }

            var textResult = await apiClient.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = input,
                MaxTokens = 500
            }, OpenAI.GPT3.ObjectModels.Models.TextDavinciV3);

            if (textResult.Successful)
            {
                var text = textResult.Choices.FirstOrDefault().Text;

                if (text.Length > 1992)
                {
                    text = text.Substring(0, 1992);
                }
                return text;
            }
            else
            {
                throw new ApplicationException($"{textResult.Error.Code}: {textResult.Error.Message}");
            }
        }

        public async Task<string> ExecuteChatGptQuery(ulong guildId, ulong channelId, ulong messageId, string input)
        {
            MessageHistory history = messageStore.DropMessageHistory(guildId, channelId, messageId);
            List<ChatMessage> messages = new List<ChatMessage>();

            foreach(Tuple<MessageType, string> message in history.Messages)
            {
                switch (message.Item1)
                {
                    case MessageType.Assistant:
                        messages.Add(ChatMessage.FromAssistance(message.Item2));
                        break;
                    case MessageType.User:
                        messages.Add(ChatMessage.FromUser(message.Item2));
                        break;
                    default:
                        throw new NotImplementedException("MessageType not supported.");
                }
            }
            messages.Add(ChatMessage.FromUser(input));

            var completionResult = await apiClient.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = messages,
                Model = OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo
            });

            if (completionResult.Successful)
            {
                SaveToMessageStore(guildId, channelId, messageId, messages, completionResult.Choices.First().Message.Content);
                return completionResult.Choices.First().Message.Content;
            } else
            {
                throw new ApplicationException($"{completionResult.Error.Code}: {completionResult.Error.Message}");
            }
        }

        public void UpdateId(ulong guildId, ulong channelId, ulong messageId, ulong newMessageId)
        {
            messageStore.UpdateId(guildId, channelId, messageId, newMessageId);
        }

        private void SaveToMessageStore(ulong guildId, ulong channelId, ulong messageId, List<ChatMessage> history, string newResponse)
        {
            MessageHistory newHistory = new MessageHistory();
            foreach (ChatMessage message in history)
            {
                switch (message.Role)
                {
                    case "assistant":
                        newHistory.Messages.Add(new Tuple<MessageType, string>(MessageType.Assistant, message.Content));
                        break;
                    case "user":
                        newHistory.Messages.Add(new Tuple<MessageType, string>(MessageType.User, message.Content));
                        break;
                    default:
                        throw new NotImplementedException("Unexpected role");
                }
            }
            newHistory.Messages.Add(new Tuple<MessageType, string>(MessageType.Assistant, newResponse));
            messageStore.PutMessageHistory(guildId, channelId, messageId, newHistory);
            File.WriteAllText(globalPath, JsonConvert.SerializeObject(messageStore));
        }    
    }
}
