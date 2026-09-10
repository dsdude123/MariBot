using System.Diagnostics.CodeAnalysis;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using MariBot.Common.Model.Discord;
using Newtonsoft.Json;
using System.Text;
using MariBot.Core.Models;
using MariBot.Core.Models.ChatGPT;
using Discord;
using MariBot.Core.Models.Election;
using MariBot.Common.Model.GpuWorker;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing LiteDB database storage and retrieval
    /// </summary>
    public class DataService
    {
        private readonly ILogger<DataService> logger;
        private readonly LiteDatabase db;

        protected DataService() { }

        public DataService(ILogger<DataService> logger, string? dbPath = null)
        {
            this.logger = logger;
            dbPath ??= Environment.GetEnvironmentVariable("DiscordSettings__DataDbPath") ?? "data.db";
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            db = new LiteDatabase(dbPath);

            logger.LogInformation("DB init in progress...");

            var col = db.GetCollection<Message>("discordMessages");
            col.EnsureIndex(x => x.Id);
            col.EnsureIndex(x => x.GuildId);
            col.EnsureIndex(x => x.ChannelId);

            var staticTextCol = db.GetCollection<StaticTextResponse>("staticTextResponses");
            staticTextCol.EnsureIndex(x => x.Id);
            staticTextCol.EnsureIndex(x => x.IsGlobal);

            var chatGptCol = db.GetCollection<MessageHistory>("chatGptHistory");
            chatGptCol.EnsureIndex(x => x.Id);

            var sleeperCol = db.GetCollection<SleeperSubscription>("sleeperSubscriptions");
            sleeperCol.EnsureIndex(x => x.Id);

            var jobMetricsCol = db.GetCollection<JobMetric>("workerJobMetrics");
            jobMetricsCol.EnsureIndex(x => x.Id);
            // Both queries this collection serves are time-bounded: the metrics
            // page reads a window, and pruning deletes everything older than one.
            jobMetricsCol.EnsureIndex(x => x.CompletedAt);

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
                var col = db.GetCollection<Message>("discordMessages");
                col.Insert(message);
            }
            catch (Exception ex)
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

                var col = db.GetCollection<Message>("discordMessages");
                return col.Query()
                    .Limit(size)
                    .Offset(page * size)
                    .ToList();

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

                var col = db.GetCollection<Message>("discordMessages");
                return col.Query()
                    .Where(x => x.AuthorId.Equals(id))
                    .Limit(size)
                    .Offset(page * size)
                    .ToList();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }

        }

        // Static Text Response Methods

        /// <summary>
        /// Gets all static text responses
        /// </summary>
        /// <returns>All static text responses</returns>
        public List<StaticTextResponse>? GetStaticTextResponse()
        {
            try
            {

                var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                return col.FindAll().ToList();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a static text response
        /// </summary>
        /// <param name="id">Id to find</param>
        /// <returns>StaticTextResponse or null if not found</returns>
        public StaticTextResponse? GetStaticTextResponse(string id)
        {
            try
            {
 
                var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                return col.FindById(id);

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

                var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                col.Insert(staticTextResponse);

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

                var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                col.Upsert(staticTextResponse);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes an existing StaticTextResponse in the DB.
        /// </summary>
        /// <param name="staticTextResponse">StaticTextResponse</param>
        /// <returns>True if successful</returns>
        public bool DeleteStaticTextResponse(StaticTextResponse staticTextResponse)
        {
            try
            {

                var col = db.GetCollection<StaticTextResponse>("staticTextResponses");
                return col.Delete(staticTextResponse.Id);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
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

                var col = db.GetCollection<MessageHistory>("chatGptHistory");
                return col.FindById(id);

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

                var col = db.GetCollection<MessageHistory>("chatGptHistory");
                col.Upsert(messageHistory);

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

                var col = db.GetCollection<MessageHistory>("chatGptHistory");
                var history = col.FindById(oldId);
                history.MessageId = newMessageId;
                col.Delete(oldId);
                col.Insert(history);
                return true;

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to update ID in DB. {}", ex.Message);
                return false;
            }
        }

        // Twitter Subscription Methods

        /// <summary>
        /// Gets a Twitter subscription
        /// </summary>
        /// <param name="id">Id to find</param>
        /// <returns>TwitterSubscription or null if not found</returns>
        public TwitterSubscription? GetTwitterSubscription(string id)
        {
            try
            {

                var col = db.GetCollection<TwitterSubscription>("twitterSubscriptions");
                return col.FindById(id);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all Twitter subscriptions
        /// </summary>
        /// <returns>IEnumerable of TwitterSubscriptions</returns>
        public IEnumerable<TwitterSubscription> GetAllTwitterSubscriptions()
        {
            try
            {

                var col = db.GetCollection<TwitterSubscription>("twitterSubscriptions");
                return col.FindAll();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Updates an existing Twitter subscription in the DB or adds a new one if it doesn't exist.
        /// </summary>
        /// <param name="twitterSubscription">TwitterSubscription</param>
        /// <returns>True if successful</returns>
        public bool UpdateTwitterSubscription(TwitterSubscription twitterSubscription)
        {
            try
            {

                var col = db.GetCollection<TwitterSubscription>("twitterSubscriptions");
                col.Upsert(twitterSubscription);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        // Sleeper Subscription Methods

        /// <summary>
        /// Gets a guild's Sleeper subscription
        /// </summary>
        /// <param name="guildId">Guild id to find</param>
        /// <returns>SleeperSubscription or null if not found</returns>
        public virtual SleeperSubscription? GetSleeperSubscription(ulong guildId)
        {
            try
            {

                var col = db.GetCollection<SleeperSubscription>("sleeperSubscriptions");
                return col.FindById(guildId.ToString());

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all Sleeper subscriptions
        /// </summary>
        /// <returns>IEnumerable of SleeperSubscriptions</returns>
        public virtual IEnumerable<SleeperSubscription> GetAllSleeperSubscriptions()
        {
            try
            {

                var col = db.GetCollection<SleeperSubscription>("sleeperSubscriptions");
                return col.FindAll();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Updates an existing Sleeper subscription in the DB or adds a new one if it doesn't exist.
        /// </summary>
        /// <param name="sleeperSubscription">SleeperSubscription</param>
        /// <returns>True if successful</returns>
        public virtual bool UpdateSleeperSubscription(SleeperSubscription sleeperSubscription)
        {
            try
            {

                var col = db.GetCollection<SleeperSubscription>("sleeperSubscriptions");
                col.Upsert(sleeperSubscription);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes a guild's Sleeper subscription.
        /// </summary>
        /// <param name="guildId">Guild id to remove</param>
        /// <returns>True if a subscription was removed</returns>
        public virtual bool DeleteSleeperSubscription(ulong guildId)
        {
            try
            {

                var col = db.GetCollection<SleeperSubscription>("sleeperSubscriptions");
                return col.Delete(guildId.ToString());

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
        }

        public Models.Election.Poll GetPoll(string id)
        {
            try
            {

                var col = db.GetCollection<Models.Election.Poll>("polls");
                return col.FindById(id);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        public IEnumerable<Models.Election.Poll> GetPollsByStatus(PollStatus status)
        {
            try
            {

                var col = db.GetCollection<Models.Election.Poll>("polls");
                return col.Query()
                    .Where(x => x.Status == status)
                    .ToList();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        public bool UpdatePoll(Models.Election.Poll poll)
        {
            try
            {

                var col = db.GetCollection<Models.Election.Poll>("polls");
                col.Upsert(poll);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        public bool DeletePoll(Models.Election.Poll poll)
        {
            try
            {

                var col = db.GetCollection< Models.Election.Poll> ("polls");
                return col.Delete(poll.Id);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
        }

        public List<Ballot> GetBallots(string pollId)
        {
            try
            {

                var col = db.GetCollection<Ballot>("ballots");
                return col.Query()
                    .Where(x => x.PollId.Equals(pollId))
                    .ToList();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        public List<Ballot> GetBallots(ulong electorId, string pollId) 
        {
            try
            {
                var voteId = $"{pollId}:{electorId}";

                var col = db.GetCollection<Ballot>("ballots");
                return col.Query()
                    .Where(x => x.VoteId.Equals(voteId))
                    .ToList();

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to read from DB. {}", ex.Message);
                return null;
            }
        }

        public bool UpdateBallot(Ballot ballot)
        {
            try
            {

                var col = db.GetCollection<Ballot>("ballots");
                col.Upsert(ballot);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
            return true;
        }

        public bool DeleteBallot(Ballot ballot)
        {
            try
            {

                var col = db.GetCollection<Ballot>("ballots");
                return col.Delete(ballot.Id);

            }
            catch (Exception ex)
            {
                logger.LogCritical("Failed to write to DB. {}", ex.Message);
                return false;
            }
        }

        // Worker Job Metrics Methods

        /// <summary>
        /// Records one finished job.
        /// </summary>
        /// <remarks>
        /// Metrics are never worth failing a job over, so this swallows and logs
        /// rather than throwing back into the dispatcher.
        /// </remarks>
        public virtual bool WriteJobMetric(JobMetric metric)
        {
            try
            {
                db.GetCollection<JobMetric>("workerJobMetrics").Insert(metric);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to record job metric. {}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Finished jobs completed at or after <paramref name="since"/>, newest first.
        /// </summary>
        public virtual IReadOnlyList<JobMetric> GetJobMetrics(DateTimeOffset since)
        {
            try
            {
                return db.GetCollection<JobMetric>("workerJobMetrics")
                    .Find(x => x.CompletedAt >= since)
                    .OrderByDescending(x => x.CompletedAt)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to read job metrics. {}", ex.Message);
                return Array.Empty<JobMetric>();
            }
        }

        /// <summary>
        /// Drops metrics older than <paramref name="before"/>.
        /// </summary>
        /// <returns>Rows removed.</returns>
        public virtual int PruneJobMetrics(DateTimeOffset before)
        {
            try
            {
                return db.GetCollection<JobMetric>("workerJobMetrics").DeleteMany(x => x.CompletedAt < before);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to prune job metrics. {}", ex.Message);
                return 0;
            }
        }

    }
}
