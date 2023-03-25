using System.Diagnostics.CodeAnalysis;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using MariBot.Common.Model.Discord;
using Newtonsoft.Json;
using System.Text;
using MariBot.Core.Models;
using MariBot.Core.Models.ChatGPT;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing LiteDB database storage and retrieval
    /// </summary>
    public class DataService
    {
        private readonly ILogger<DataService> logger;

        public DataService(ILogger<DataService> logger)
        {
            this.logger = logger;

            logger.LogInformation("DB init in progress...");
            using (var db = new LiteDatabase("data.db"))
            {
                var col = db.GetCollection<Message>("discordMessages");
                col.EnsureIndex(x => x.Id);
                col.EnsureIndex(x => x.GuildId);
                col.EnsureIndex(x => x.ChannelId);

                var staticTextCol = db.GetCollection<StaticTextResponse>("staticTextResponses");
                staticTextCol.EnsureIndex(x => x.Id);
                staticTextCol.EnsureIndex(x => x.IsGlobal);

                var chatGptCol = db.GetCollection<MessageHistory>("chatGptHistory");
                chatGptCol.EnsureIndex(x => x.Id);
            }
        }

        // Discord Message Methods

        /// <summary>
        /// Saves a Discord message to the DB
        /// </summary>
        /// <param name="context">SocketCommandContext of the message</param>
        public void WriteDiscordMessage(SocketCommandContext context)
        {
            var itemToSave = Message.FromSocketCommandContext(context);
            InsertMessage(itemToSave);
        }

        /// <summary>
        /// Saves a Message to the DB
        /// </summary>
        /// <param name="message">Message object</param>
        public void InsertMessage(Message message)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<Message>("discordMessages");
                    col.Insert(message);
                }
            } catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
            }

        }

        /// <summary>
        /// Gets all messages from the DB
        /// </summary>
        /// <param name="size">Max messages to return per page</param>
        /// <param name="page">Page to return</param>
        /// <returns>List of messages</returns>
        public List<Message> GetAll(int size, int page)
        {

            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<Message>("discordMessages");
                    return col.Query()
                        .Limit(size)
                        .Offset(page * size)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }

        }

        /// <summary>
        /// Gets all messages belong to a specific author
        /// </summary>
        /// <param name="id">Author ID</param>
        /// <param name="size">Max messages to return per page</param>
        /// <param name="page">Page to return</param>
        /// <returns></returns>
        public List<Message> GetByAuthorId(ulong id, int size, int page)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<Message>("discordMessages");
                    return col.Query()
                        .Where(x => x.AuthorId.Equals(id))
                        .Limit(size)
                        .Offset(page * size)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }

        }

        // Static Text Response Methods

        /// <summary>
        /// Gets a static text response
        /// </summary>
        /// <param name="id">Id to find</param>
        /// <returns>StaticTextResponse or null if not found</returns>
        public StaticTextResponse? GetStaticTextResponse(string id)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                    return col.FindById(id);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Writes a new StaticTextResponse to the DB
        /// </summary>
        /// <param name="staticTextResponse">New StaticTextResponse</param>
        /// <returns>True if successful</returns>
        public bool InsertStaticTextResponse(StaticTextResponse staticTextResponse)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                    col.Insert(staticTextResponse);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Updates an existing StaticTextResponse in the DB or adds a new one if it doesn't exist.
        /// </summary>
        /// <param name="staticTextResponse">StaticTextResponse</param>
        /// <returns>True if successful</returns>
        public bool UpdateStaticTextResponse(StaticTextResponse staticTextResponse)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                    col.Upsert(staticTextResponse);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        // ChatGPT Methods

        /// <summary>
        /// Gets ChatGPT Message History from the DB
        /// </summary>
        /// <param name="guildId">Guild ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="messageId">Message ID</param>
        /// <returns>MessageHistory or null if not found</returns>
        public MessageHistory? GetChatGptMessageHistory(ulong guildId, ulong channelId, ulong messageId)
        {
            var id = $"{guildId}-{channelId}-{messageId}";
            logger.LogDebug("Trying to get chatgpt {}", id);
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<MessageHistory>("chatGptHistory");
                    return col.FindById(id);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Updates an existing Chat GPT message history in the DB or adds a new one if it doesn't exist.
        /// </summary>
        /// <param name="messageHistory">Chat GPT message history</param>
        /// <returns>True if successful</returns>
        public bool UpdateChatGptMessageHistory(MessageHistory messageHistory)
        {
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<MessageHistory>("chatGptHistory");
                    col.Upsert(messageHistory);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Updates ID of Chat GPT Message History
        /// </summary>
        /// <param name="guildId">Guild ID</param>
        /// <param name="channelId">Channel ID</param>
        /// <param name="messageId">Old message ID</param>
        /// <param name="newMessageId">New message ID</param>
        /// <returns>True if successful</returns>
        public bool UpdateChatGptMessageHistoryId(ulong guildId, ulong channelId, ulong messageId, ulong newMessageId)
        {
            var oldId = $"{guildId}-{channelId}-{messageId}";
            logger.LogDebug("Old id {}", oldId);
            try
            {
                using (var db = new LiteDatabase("data.db"))
                {
                    var col = db.GetCollection<MessageHistory>("chatGptHistory");
                    var history = col.FindById(oldId);
                    history.MessageId = newMessageId;
                    col.Delete(oldId);
                    col.Insert(history);
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to update ID in DB. {}", ex.Message);
                return false;
            }
        }
    }
}
