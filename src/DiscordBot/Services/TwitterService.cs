using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class TwitterService
    {
        private static readonly string ApiKey = DiscordBot.Program._config["twitterApiKey"];
        private static readonly string ApiKeySecret = DiscordBot.Program._config["twitterApiKeySecret"];

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
    }
}
