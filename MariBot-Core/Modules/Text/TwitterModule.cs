using System.Timers;
using Discord;
using Discord.Commands;
using LinqToTwitter;
using MariBot.Core.Models;
using MariBot.Core.Services;
using MariBot.Services;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Modules.Text
{
    [Group("twitter")]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        private DataService dataService { get; set; }
        private TwitterService twitterService { get; set; }

        private static EmbedAuthorBuilder TwitterLogo = new EmbedAuthorBuilder()
            .WithName("Twitter")
            .WithIconUrl("https://img.icons8.com/color/48/000000/twitter--v1.png");
        private static EmbedFooterBuilder TwitterFooter = new EmbedFooterBuilder()
            .WithText("Twitter")
            .WithIconUrl("https://img.icons8.com/color/48/000000/twitter--v1.png");

        public TwitterModule(DataService dataService, TwitterService twitterService)
        {
            this.dataService = dataService;
            this.twitterService = twitterService;
        }

        [Command("get-user", RunMode = RunMode.Async)]
        public async Task GetUser(string username)
        {
            var eb = new EmbedBuilder();
            try
            {
                User user = twitterService.GetTwitterUser(username);
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
            Status tweet = twitterService.GetTweet(id);

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
            TwitterSubscription subscription = dataService.GetTwitterSubscription(username);

            if (subscription != null)
            {
                // Subscription already exists so let's just add our channel as a destination
                if (subscription.GuildChannelSubscriptions.ContainsKey(Context.Guild.Id))
                {
                    subscription.GuildChannelSubscriptions[Context.Guild.Id].Add(Context.Channel.Id);
                }
                else
                {
                    subscription.GuildChannelSubscriptions.Add(Context.Guild.Id, new HashSet<ulong> { Context.Channel.Id });
                }

                var result = dataService.UpdateTwitterSubscription(subscription)
                    ? "Subscription created."
                    : "Failed to create subscription.";
                await ReplyAsync(result);
            }
            else
            {
                try
                {
                    List<Status> tweets = twitterService.GetLatestUserTweets(username);
                    TwitterSubscription newSubsciption = new TwitterSubscription();
                    newSubsciption.GuildChannelSubscriptions = new Dictionary<ulong, HashSet<ulong>>();
                    newSubsciption.GuildChannelSubscriptions.Add(Context.Guild.Id, new HashSet<ulong> { Context.Channel.Id });
                    newSubsciption.PostedIds = new HashSet<ulong>();
                    tweets.ForEach(tweet => newSubsciption.PostedIds.Add(tweet.StatusID));
                    var result = dataService.UpdateTwitterSubscription(newSubsciption)
                        ? "Subscription created."
                        : "Failed to create subscription.";
                    await ReplyAsync(result);
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
            TwitterSubscription subscription = dataService.GetTwitterSubscription(username);

            if (subscription != null)
            {
                if (subscription.GuildChannelSubscriptions.ContainsKey(Context.Guild.Id)
                    && subscription.GuildChannelSubscriptions[Context.Guild.Id].Contains(Context.Channel.Id))
                {
                    subscription.GuildChannelSubscriptions[Context.Guild.Id].Remove(Context.Channel.Id);
                    var result = dataService.UpdateTwitterSubscription(subscription)
                        ? "Subscription removed."
                        : "Failed to remove subscription.";
                    await ReplyAsync(result);
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
    }
}
