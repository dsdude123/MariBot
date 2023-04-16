using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using MariBot.Core.Models;
using MariBot.Core.Services;
using Timer = System.Timers.Timer;

namespace MariBot.Services
{
    public class TwitterService
    {
        private readonly string ApiKey;
        private readonly string ApiKeySecret;
        private readonly DiscordSocketClient discord;

        private DataService dataService { get; set; }
        
        private System.Timers.Timer CheckTimer = new Timer { AutoReset = true, Enabled = true, Interval = 60000 };

        private static EmbedFooterBuilder TwitterFooter = new EmbedFooterBuilder()
            .WithText("Twitter")
            .WithIconUrl("https://img.icons8.com/color/48/000000/twitter--v1.png");

        public TwitterService(IConfiguration configuration, DataService dataService, DiscordSocketClient discord)
        {
            ApiKey = configuration["DiscordSettings:TwitterApiKey"];
            ApiKeySecret = configuration["DiscordSettings:TwitterApiKeySecret"];
            this.dataService = dataService;
            this.discord = discord;
        }

        public User GetTwitterUser(string username)
        {
            var context = GetTwitterContext().Result;

            var userResponse =
                (from user in context.User
                 where user.Type == UserType.Show &&
                 user.ScreenName == username
                 select user).First();

            return userResponse;
        }

        public List<Status> GetLatestUserTweets(string username)
        {
            var context = GetTwitterContext().Result;

            var tweets =
                (from tweet in context.Status
                 where tweet.Type == StatusType.User &&
                 tweet.ScreenName == username &&
                 tweet.Count == 100 &&
                 tweet.SinceID == 1 &&
                 tweet.TweetMode == TweetMode.Extended &&
                 tweet.IncludeEntities == true
                 orderby tweet.CreatedAt
                 select tweet)
                .ToListAsync().Result;

            return tweets;
        }


        public Status GetTweet(ulong id)
        {
            var context = GetTwitterContext().Result;

            var tweetResult =
                (from tweet in context.Status
                 where tweet.Type == StatusType.Show &&
                 tweet.ID == id &&
                 tweet.TweetMode == TweetMode.Extended &&
                 tweet.IncludeEntities == true
                 select tweet).Single();

            return tweetResult;
        }

        private async Task<TwitterContext> GetTwitterContext()
        {
            var auth = new ApplicationOnlyAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore()
                {
                    ConsumerKey = ApiKey,
                    ConsumerSecret = ApiKeySecret
                }
            };

            await auth.AuthorizeAsync();
            return new TwitterContext(auth);
        }

        public void HandleTimer(object source, ElapsedEventArgs arg)
        {
            var subscriptions = dataService.GetAllTwitterSubscriptions();

            foreach (TwitterSubscription user in subscriptions)
            {
                List<Status> tweets = GetLatestUserTweets(user.Username);
                List<Status> toPost = new List<Status>();

                foreach (var tweet in tweets)
                {
                    if (!user.PostedIds.Contains(tweet.StatusID))
                    {
                        toPost.Add(tweet);
                    }
                }

                foreach (var tweet in toPost)
                {
                    var eb = new EmbedBuilder();
                    EmbedAuthorBuilder tweetAuthor = new EmbedAuthorBuilder()
                        .WithName(tweet.User.Name)
                        .WithIconUrl(tweet.User.ProfileImageUrl);

                    eb.WithAuthor(tweetAuthor);
                    eb.WithColor(Color.Blue);
                    eb.WithTimestamp(tweet.CreatedAt);
                    eb.WithFooter(TwitterFooter);
                    eb.WithDescription(tweet.FullText);

                    if (tweet.ExtendedEntities.MediaEntities != null && tweet.ExtendedEntities.MediaEntities.Count > 0)
                    {
                        var mediaEntity = tweet.ExtendedEntities.MediaEntities[0];
                        eb.WithImageUrl(mediaEntity.MediaUrl);
                    }

                    foreach (var guild in user.GuildChannelSubscriptions.Keys)
                    {
                        foreach (var channel in user.GuildChannelSubscriptions[guild])
                        {
                            IGuild server = FindServer(guild);
                            ITextChannel ch = FindTextChannel(server, channel);

                            ch.SendMessageAsync("", false, eb.Build());
                        }
                    }

                    user.PostedIds.Add(tweet.StatusID);
                }

                dataService.UpdateTwitterSubscription(user);
            }
        }

        private IGuild FindServer(ulong id)
        {
            foreach (IGuild server in discord.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        private ITextChannel FindTextChannel(IGuild server, ulong id)
        {
            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }
    }
}
