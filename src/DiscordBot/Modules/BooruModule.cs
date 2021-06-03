using Discord;
using Discord.Commands;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        public BooruService booruService { get; set; }
        public PictureService pictureService { get; set; }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("danbooru")]
        public Task danbooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomDanbooruDonmai(tagArray);
                return commonBooruResponse(post);
            } catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("gelbooru")]
        public Task gelbooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomGelbooru(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("konachan")]
        public Task konachan([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomKonachan(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("realbooru")]
        public Task realbooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomRealbooru(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("r34")]
        public Task r34([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomRule34(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [DisallowSomeServers]
        [Command("safebooru")]
        public Task safebooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomSafebooru(tagArray);
                assertSafePost(post);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [DisallowSomeServers]
        [Command("sakugabooru")]
        public Task sakugabooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomSakugabooru(tagArray);
                assertSafePost(post);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("sankakucomplex")]
        public Task sankakucomplex([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomSankakuComplex(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            { 
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("xbooru")]
        public Task xbooru([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomXbooru(tagArray);
                return commonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return commonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [DisallowSomeServers]
        [Command("yandere")]
        public async Task yandere([Remainder] string tags = null)
        {
            string[] tagArray = getTagArray(tags);
            try
            {
                var post = booruService.getRandomYandere(tagArray);
                var embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle(post.id.ToString());
                embedBuilder.WithDescription(getDescription(post.score, post.rating, post.tags));
                embedBuilder.WithColor(Color.Gold);
                string postUrl = post.postUrl.AbsoluteUri.ToString();
                embedBuilder.WithUrl(postUrl);

                if (post.creation != null)
                {
                    DateTimeOffset time = new DateTimeOffset((DateTime)post.creation);
                    embedBuilder.WithTimestamp(time);
                }

                await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
                var image = pictureService.GetPictureAsync(post.fileUrl.AbsoluteUri.ToString()).Result;
                await Context.Channel.SendFileAsync(image, "yandere.jpg");
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                commonNoResultsResponse();
            }
        }

        private void assertSafePost(BooruSharp.Search.Post.SearchResult post)
        {
            if(!post.rating.Equals(BooruSharp.Search.Post.Rating.Safe))
            {
                throw new ArgumentOutOfRangeException("Booru post is not marked as safe.");
            }
        }

        private Task commonBooruResponse(BooruSharp.Search.Post.SearchResult post)
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(post.id.ToString());
            embedBuilder.WithDescription(getDescription(post.score, post.rating, post.tags));
            embedBuilder.WithColor(Color.Gold);
            string postUrl = post.postUrl.AbsoluteUri.ToString();
            embedBuilder.WithUrl(postUrl);
            string imageUrl = post.fileUrl.AbsoluteUri.ToString();
            embedBuilder.WithImageUrl(imageUrl);

            if (post.creation != null)
            {
                DateTimeOffset time = new DateTimeOffset((DateTime)post.creation);
                embedBuilder.WithTimestamp(time);
            }

            return Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private Task commonNoResultsResponse()
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithDescription("No results were found");
            embedBuilder.WithColor(Color.Red);

            return Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private string getDescription(int? score, BooruSharp.Search.Post.Rating rating, string[] tags)
        {
            string returnValue = "";

            returnValue += "Rating: " + rating.ToString() + "\n";
            if(score != null) returnValue += "Score: " + score + "\n";

            returnValue += "\nTags:\n";
            foreach(string tag in tags)
            {
                returnValue += "`" + tag + "`" + " ";
            }
            return returnValue;
        }

        private string[] getTagArray(string tags)
        {
            if (tags != null)
            {
                return tags.Split(' ');
            }
            else
            {
                return new string[0];
            }
        }
    }
}
