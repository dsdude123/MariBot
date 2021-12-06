using Discord;
using Discord.Commands;
using DiscordBot;
using LinqToTwitter;
using MariBot.Models;
using MariBot.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MariBot.Modules
{
    [Group("twitter")]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        public static TwitterService TwitterService { get; set; }
        public static Timer CheckTimer = new Timer { AutoReset = true, Enabled = true, Interval = 60000 };
        private static string globalPath = Environment.CurrentDirectory + "\\data\\global\\twitterSubscriptions.json";

        public static EmbedAuthorBuilder TwitterLogo = new EmbedAuthorBuilder()
            .WithName("Twitter")
            .WithIconUrl("https://img.icons8.com/color/48/000000/twitter--v1.png");
        public static EmbedFooterBuilder TwitterFooter = new EmbedFooterBuilder()
            .WithText("Twitter")
            .WithIconUrl("https://img.icons8.com/color/48/000000/twitter--v1.png");

        public TwitterModule()
        {
            TwitterService = new TwitterService();
            CheckTimer.Elapsed += HandleTimer;
        }

        [Command("get-user", RunMode = RunMode.Async)]
        public async Task GetUser(string username)
        {
            var eb = new EmbedBuilder();
            try
            {
                User user = TwitterService.GetTwitterUser(username);
                eb.WithTitle(user.Name);
                eb.WithColor(Color.Blue);
                eb.WithThumbnailUrl(user.ProfileImageUrl);
                eb.WithUrl(user.Url);
                eb.WithAuthor(TwitterLogo);

                string description = user.Description
                    + "\n\n"
                    + "**Location: **" + user.Location + "\n"
                    + "**Followers:** " + user.FollowersCount;
                eb.WithDescription(description);
                await Context.Channel.SendMessageAsync("", false, eb.Build());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TwitterQueryException)
                {
                    eb.WithTitle("Twitter");
                    eb.WithDescription(ex.InnerExceptions[0].Message);
                    eb.WithColor(Color.Red);
                    eb.WithAuthor(TwitterLogo);
                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                }
                else
                {
                    throw ex; // Rethrow since not expected inner exception
                }
            }
        }

        [Command("get-tweet", RunMode = RunMode.Async)]
        public async Task GetTweet(ulong id)
        {
            Status tweet = TwitterService.GetTweet(id);

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

            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [RequireUserPermission(GuildPermission.ManageWebhooks)] // Not actually a webhook but since it operates in a similar way this works
        [Command("subscribe", RunMode = RunMode.Async)]
        public async Task SubscribeTwitter(string username)
        {
            Dictionary<string, TwitterSubscription> subscriptions;

            if (System.IO.File.Exists(globalPath))
            {
                subscriptions = JsonConvert.DeserializeObject<Dictionary<string, TwitterSubscription>>(
                    File.ReadAllText(globalPath));
            }
            else
            {
                subscriptions = new Dictionary<string, TwitterSubscription>();
            }

            if (subscriptions.ContainsKey(username))
            {
                // Subscription already exists so let's just add our channel as a destination
                if (subscriptions[username].guildChannelSubscriptions.ContainsKey(Context.Guild.Id))
                {
                    subscriptions[username].guildChannelSubscriptions[Context.Guild.Id].Add(Context.Channel.Id);
                }
                else
                {
                    subscriptions[username].guildChannelSubscriptions.Add(Context.Guild.Id, new HashSet<ulong> { Context.Channel.Id });
                }
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(subscriptions));
                await ReplyAsync("Subscription created.");
            }
            else
            {
                try
                {
                    List<Status> tweets = TwitterService.GetLatestUserTweets(username);
                    TwitterSubscription newSubsciption = new TwitterSubscription();
                    newSubsciption.guildChannelSubscriptions = new Dictionary<ulong, HashSet<ulong>>();
                    newSubsciption.guildChannelSubscriptions.Add(Context.Guild.Id, new HashSet<ulong> { Context.Channel.Id });
                    newSubsciption.postedIds = new HashSet<ulong>();
                    tweets.ForEach(tweet => newSubsciption.postedIds.Add(tweet.StatusID));
                    subscriptions[username] = newSubsciption;
                    File.WriteAllText(globalPath, JsonConvert.SerializeObject(subscriptions));
                    await ReplyAsync("Subscription created.");
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TwitterQueryException)
                    {
                        await ReplyAsync(ex.InnerExceptions[0].Message);
                    }
                    else
                    {
                        throw ex; // Rethrow since not expected inner exception
                    }
                }
            }
        }

        [RequireUserPermission(GuildPermission.ManageWebhooks)] // Not actually a webhook but since it operates in a similar way this works
        [Command("unsubscribe", RunMode = RunMode.Async)]
        public async Task UnsubscribeTwitter(string username)
        {
            Dictionary<string, TwitterSubscription> subscriptions;

            if (System.IO.File.Exists(globalPath))
            {
                subscriptions = JsonConvert.DeserializeObject<Dictionary<string, TwitterSubscription>>(
                    File.ReadAllText(globalPath));
            }
            else
            {
                await ReplyAsync("No subscription exists.");
                return;
            }

            if (subscriptions.ContainsKey(username))
            {
                if (subscriptions[username].guildChannelSubscriptions.ContainsKey(Context.Guild.Id)
                    && subscriptions[username].guildChannelSubscriptions[Context.Guild.Id].Contains(Context.Channel.Id))
                {
                    subscriptions[username].guildChannelSubscriptions[Context.Guild.Id].Remove(Context.Channel.Id);
                    File.WriteAllText(globalPath, JsonConvert.SerializeObject(subscriptions));
                    await ReplyAsync("Subscription removed.");
                }
                else
                {
                    await ReplyAsync("No subscription exists.");
                }
            }
            else
            {
                await ReplyAsync("No subscription exists.");
            }
        }

        public static void HandleTimer(object source, ElapsedEventArgs arg)
        {
            Dictionary<string, TwitterSubscription> subscriptions;

            if (System.IO.File.Exists(globalPath))
            {
                subscriptions = JsonConvert.DeserializeObject<Dictionary<string, TwitterSubscription>>(
                    File.ReadAllText(globalPath));
            }
            else
            {
                return;
            }

            foreach (string username in subscriptions.Keys)
            {
                List<Status> tweets = TwitterService.GetLatestUserTweets(username);
                List<Status> toPost = new List<Status>();

                foreach(var tweet in tweets)
                {
                    if (!subscriptions[username].postedIds.Contains(tweet.StatusID))
                    {
                        toPost.Add(tweet);
                    }
                }

                foreach(var tweet in toPost)
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

                    if(tweet.ExtendedEntities.MediaEntities != null && tweet.ExtendedEntities.MediaEntities.Count > 0)
                    {
                        var mediaEntity = tweet.ExtendedEntities.MediaEntities[0];
                        eb.WithImageUrl(mediaEntity.MediaUrl);
                    }

                    foreach (var guild in subscriptions[username].guildChannelSubscriptions.Keys)
                    {
                        foreach(var channel in subscriptions[username].guildChannelSubscriptions[guild])
                        {
                            IGuild server = findServer(guild);
                            ITextChannel ch = findTextChannel(server, channel);
                            
                            ch.SendMessageAsync("", false, eb.Build());
                        }
                    }

                    subscriptions[username].postedIds.Add(tweet.StatusID);
                }
            }

            File.WriteAllText(globalPath, JsonConvert.SerializeObject(subscriptions));
        }

        static IGuild findServer(ulong id)
        {
            foreach (IGuild server in Program._client.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        static ITextChannel findTextChannel(IGuild server, ulong id)
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
