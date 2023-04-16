using System.Collections.ObjectModel;
using Discord;
using Discord.Commands;
using MariBot.Core.Services;
using MariBot.Services;

namespace MariBot.Core.Modules.Text
{
    public class BooruModule : ModuleBase<SocketCommandContext>
    {
        public BooruService booruService { get; set; }
        public ImageService imageService { get; set; }

        public BooruModule(BooruService booruService, ImageService imageService)
        {
            this.booruService = booruService;
            this.imageService = imageService;
        }

        [RequireNsfw]
        [Command("danbooru")]
        public Task danbooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomDanbooruDonmai(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("gelbooru")]
        public Task gelbooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomGelbooru(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("konachan")]
        public Task konachan([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomKonachan(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("realbooru")]
        public Task realbooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomRealbooru(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("r34")]
        public Task r34([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomRule34(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [Command("safebooru")]
        public Task safebooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomSafebooru(tagArray);
                AssertSafePost(post);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [Command("sakugabooru")]
        public Task sakugabooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomSakugabooru(tagArray);
                AssertSafePost(post);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("sankakucomplex")]
        public Task sankakucomplex([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomSankakuComplex(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("xbooru")]
        public Task xbooru([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomXbooru(tagArray);
                return CommonBooruResponse(post);
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                return CommonNoResultsResponse();
            }
        }

        [RequireNsfw]
        [Command("yandere")]
        public async Task yandere([Remainder] string tags = null)
        {
            string[] tagArray = GetTagArray(tags);
            try
            {
                var post = booruService.getRandomYandere(tagArray);
                var embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle(post.ID.ToString());
                embedBuilder.WithDescription(GetDescription(post.Score, post.Rating, post.Tags));
                embedBuilder.WithColor(Color.Gold);
                string postUrl = post.PostUrl.AbsoluteUri;
                embedBuilder.WithUrl(postUrl);

                if (post.Creation != null)
                {
                    DateTimeOffset time = new DateTimeOffset((DateTime)post.Creation);
                    embedBuilder.WithTimestamp(time);
                }

                await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
                var image = imageService.GetWebResource(post.FileUrl.AbsoluteUri).Result;
                await Context.Channel.SendFileAsync(image, filename: "yandere.jpg");
            }
            catch (AggregateException ex) when (ex.InnerException is BooruSharp.Search.InvalidTags)
            {
                CommonNoResultsResponse();
            }
        }

        private void AssertSafePost(BooruSharp.Search.Post.SearchResult post)
        {
            if (!post.Rating.Equals(BooruSharp.Search.Post.Rating.Safe))
            {
                throw new ArgumentOutOfRangeException("Booru post is not marked as safe.");
            }
        }

        private Task CommonBooruResponse(BooruSharp.Search.Post.SearchResult post)
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(post.ID.ToString());
            embedBuilder.WithDescription(GetDescription(post.Score, post.Rating, post.Tags));
            embedBuilder.WithColor(Color.Gold);
            string postUrl = post.PostUrl.AbsoluteUri;
            embedBuilder.WithUrl(postUrl);
            string imageUrl = post.FileUrl.AbsoluteUri;
            embedBuilder.WithImageUrl(imageUrl);

            if (post.Creation != null)
            {
                DateTimeOffset time = new DateTimeOffset((DateTime)post.Creation);
                embedBuilder.WithTimestamp(time);
            }

            return Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private Task CommonNoResultsResponse()
        {
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithDescription("No results were found");
            embedBuilder.WithColor(Color.Red);

            return Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        private string GetDescription(int? score, BooruSharp.Search.Post.Rating rating, ReadOnlyCollection<string> tags)
        {
            string returnValue = "";

            returnValue += "Rating: " + rating.ToString() + "\n";
            if (score != null) returnValue += "Score: " + score + "\n";

            returnValue += "\nTags:\n";
            foreach (string tag in tags)
            {
                returnValue += "`" + tag + "`" + " ";
            }
            return returnValue;
        }

        private string[] GetTagArray(string tags)
        {
            return tags != null ? tags.Split(' ') : Array.Empty<string>();
        }
    }
}
