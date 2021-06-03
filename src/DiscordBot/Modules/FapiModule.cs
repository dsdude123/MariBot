using Discord;
using Discord.Commands;
using MariBot.Models.FAPI;
using MariBot.Models.FAPI._4chan;
using MariBot.Models.FAPI.DuckDuckGo;
using MariBot.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    public class FapiModule : ModuleBase<SocketCommandContext>
    {
        public FapiService service { get; set; }

        //[RequireNsfw]
        //[ProhibitBlacklistedServers]
        //[Command("4chan", RunMode = RunMode.Async)]
        //public async Task fourchan(string path = null)
        //{
        //    try
        //    {
        //        Models.FAPI._4chan.ApiResponse response = null;
        //        try
        //        {
        //            var rawResponse = service.ExecutePathRequest("4chan", path).Result;
        //            response = JsonConvert.DeserializeObject<Models.FAPI._4chan.ApiResponse>(rawResponse);
        //        }
        //        catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
        //        {
        //            HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
        //            if (caught.statusCode.Equals(429))
        //            {
        //                await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
        //            }
        //            else
        //            {
        //                throw ex;
        //            }
        //        }

        //        DateTimeOffset threadTime = DateTimeOffset.FromUnixTimeMilliseconds(response.thread.time);
        //        EmbedBuilder embedBuilder = new EmbedBuilder();
        //        embedBuilder.WithTitle("/" + response.board.name + "/ - " + response.board.title);
        //        embedBuilder.WithAuthor(response.thread.id.ToString());
        //        embedBuilder.WithTimestamp(threadTime);
        //        embedBuilder.WithColor(Color.LightOrange);
        //        embedBuilder.WithUrl("https://boards.4chan.org/" + response.board.name + "/thread/" + response.thread.id);

        //        String threadText = "";
        //        String thumbnailUrl = null;
        //        foreach(Post post in response.posts)
        //        {
        //            if (post.file != null && post.file.Length > 0 && thumbnailUrl == null)
        //            {
        //                thumbnailUrl = post.file; // Get first image as the thumbnail
        //            }
        //            String postText = build4ChanPostString(post);
        //            if ((threadText + postText).Length <= 2048)
        //            {
        //                threadText += postText;
        //            }
        //        }

        //        embedBuilder.WithDescription(threadText);
        //        if (thumbnailUrl != null) embedBuilder.WithThumbnailUrl(thumbnailUrl);

        //        await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
        //    } catch (Exception ex)
        //    {
        //        await Context.Channel.SendMessageAsync("Failed to get thread from 4chan. " +
        //            "If you specified a board or thread, make sure it is in the `board` or `board/thread` format " +
        //            "replacing `board` with the letters of the board (i.e. `vp` ) and `thread` with the number of the thread (i.e. `123456`).");
        //    }
        //}

        //[Command("autism", RunMode = RunMode.Async)]
        //public async Task Autism([Remainder] string url = null)
        //{
        //    await simpleImageRequest(Context, "autism", url);
        //}

        //[Command("bandicam", RunMode = RunMode.Async)]
        //public async Task Bandicam([Remainder] string url = null)
        //{
        //    await simpleImageRequest(Context, "bandicam", url);
        //}

        //[Command("blackify", RunMode = RunMode.Async)]
        //public async Task Blackify([Remainder] string url = null)
        //{
        //    try
        //    {
        //        await simpleImageRequest(Context, "blackify", url);
        //    }
        //    catch (AggregateException e) when (e.InnerException is HttpStatusCodeException)
        //    {
        //        HttpStatusCodeException caught = (HttpStatusCodeException)e.InnerException;
        //        if (caught.statusCode.Equals(400))
        //        {
        //            await Context.Channel.SendMessageAsync("400 Bad Request. fAPI may have not been able to detect a face in the picture");
        //        }
        //    }
        //}

        //[Command("blackpanther", RunMode = RunMode.Async)]
        //public async Task BlackPanther([Remainder] string url = null)
        //{
        //    await simpleImageRequest(Context, "blackpanther", url);
        //}

        //[Command("buzzfeed", RunMode = RunMode.Async)]
        //public async Task BuzzFeed([Remainder] string text = null)
        //{
        //    await simpleImageFromTextRequest(Context, "buzzfeed", text);
        //}

        //[Command("composite", RunMode = RunMode.Async)]
        //public async Task Composite([Remainder] string url = null)
        //{
        //    await simpleImageRequest(Context, "composite", url);
        //}

        //[Command("consent", RunMode = RunMode.Async)]
        //public async Task Consent([Remainder] string text)
        //{
        //    await imageRequestWithArguments(Context, "consent", text, text);
        //}

        //[Command("coolguy", RunMode = RunMode.Async)]
        //public async Task Coolguy([Remainder] string url = null)
        //{
        //    await simpleImageRequest(Context, "coolguy", url);
        //}

        //[Command("days", RunMode = RunMode.Async)]
        //public async Task Days([Remainder] string text)
        //{
        //    await simpleImageFromTextRequest(Context, "days", text);
        //}

        [Command("depression", RunMode = RunMode.Async)]
        public async Task Depression([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "depression", url);
        }

        [Command("disabled", RunMode = RunMode.Async)]
        public async Task Disabled([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "disabled", url);
        }

        [Command("dork", RunMode = RunMode.Async)]
        public async Task Dork([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "dork", url);
        }

        [Command("duckduckgo", RunMode = RunMode.Async)]
        public async Task DuckDuckGo([Remainder] string text)
        {
            FapiRequest request = new FapiRequest();
            Arguments arguments = new Arguments();
            arguments.text = text;
            request.args = arguments;

            Models.FAPI.DuckDuckGo.ApiResponse response = null;
            try
            {
                var rawResponse = service.ExecuteTextRequest("duckduckgo", request).Result;
                response = JsonConvert.DeserializeObject<Models.FAPI.DuckDuckGo.ApiResponse>(rawResponse);
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Results for: " + text);
            embedBuilder.WithColor(Color.Orange);

            String completeText = "";
            foreach (Result result in response.results)
            {
                String resultText = buildDuckDuckGoResultString(result);
                if ((completeText + resultText).Length <= 2048)
                {
                    completeText += resultText;
                }
            }

            embedBuilder.WithDescription(completeText);

            await Context.Channel.SendMessageAsync("", false, embedBuilder.Build());
        }

        [Command("duckduckgoimages", RunMode = RunMode.Async)]
        public async Task DuckDuckGoImages([Remainder] string text)
        {
            FapiRequest request = new FapiRequest();
            Arguments arguments = new Arguments();
            arguments.text = text;
            request.args = arguments;
            String rawResponse = null;
            try
            {
                rawResponse = service.ExecuteTextRequest("duckduckgoimages", request).Result;
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
            rawResponse = "{ images:" + rawResponse + "}"; // JSON response is malformed, fix it
            Models.FAPI.DuckDuckGo.ApiResponse response = JsonConvert.DeserializeObject<Models.FAPI.DuckDuckGo.ApiResponse>(rawResponse);

            if (response.images != null && response.images.Length > 0)
            {
                Random random = new Random();
                int imageToPick = random.Next(0, response.images.Length);
                await Context.Channel.SendMessageAsync(response.images[imageToPick], false);
            } else
            {
                await Context.Channel.SendMessageAsync("No image results found.", false);
            }
        }

        [Command("edges", RunMode = RunMode.Async)]
        public async Task Edges([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "edges", url);
        }

        [Command("e2e", RunMode = RunMode.Async)]
        public async Task Edges2Emojis([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "edges2emojis", url);
        }

        [RequireNsfw]
        [Command("e2p", RunMode = RunMode.Async)]
        public async Task Edges2Porn([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "edges2porn", url);
        }

        [RequireNsfw]
        [Command("e2pg", RunMode = RunMode.Async)]
        public async Task Edges2PornGif([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "edges2porn_gif", url);
        }

        [Command("emojify", RunMode = RunMode.Async)]
        public async Task Emojify(String foregroundEmoji, String backgroundEmoji, bool vertical, [Remainder] String text)
        {
            // API documentation specifies an image argument but not clear use why as the returned result is just the text argument as emoji

            //String url = null;
            //try
            //{
            //    url = getImageUrl(Context, url);
            //}
            //catch (NotSupportedException e)
            //{
            //    await Context.Channel.SendMessageAsync(e.Message);
            //    return;
            //}

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            //List<string> images = new List<string>();
            //images.Add(url);
            //body.images = images;

            arguments.text = text;
            arguments.foreground = foregroundEmoji;
            arguments.background = backgroundEmoji;
            arguments.vertical = vertical;
            body.args = arguments;

            try
            {
                var response = service.ExecuteTextRequest("emojify", body).Result;
                await Context.Channel.SendMessageAsync(response);
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("emojimosaic", RunMode = RunMode.Async)]
        public async Task EmojiMosaic(int gridSize = 32)
        {
            // TODO: Add validation to make sure value of grid size is within accepted range
            await imageRequestWithArguments(Context, "emojimosaic", gridSize.ToString());
        }

        [Command("EvalMagik", RunMode = RunMode.Async)]
        public async Task EvalMagik([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "evalmagik", text);
        }

        [Command("excuse", RunMode = RunMode.Async)]
        public async Task Excuse([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "excuse", url);
        }

        [Command("eyes", RunMode = RunMode.Async)]
        public async Task Eyes(String eyeType)
        {
            // TODO: Add validation to make sure value of text is within accepted range
            //The eye overlay to use (one of big, black, blood, blue, googly, green, horror, 
            //illuminati, money, normal, pink, red, small, spinner, spongebob, white, yellow, lucille)
            await imageRequestWithArguments(Context, "eyes", eyeType);
        }

        // As of the time of implementation (10/25/2020), the API endpoint for this doesn't seem to be working.
        [Command("faceapp", RunMode = RunMode.Async)]
        public async Task FaceApp(String text = null)
        {
            try
            {
                if (text == null)
                {
                    String url = null;
                    try
                    {
                        url = getImageUrl(Context, url);
                    }
                    catch (NotSupportedException e)
                    {
                        await Context.Channel.SendMessageAsync(e.Message);
                        return;
                    }

                    FapiRequest body = new FapiRequest();
                    List<string> images = new List<string>();
                    images.Add(url);
                    body.images = images;

                    var response = service.ExecuteTextRequest("faceapp", body).Result;
                    await Context.Channel.SendMessageAsync(response);
                }
                else
                {
                    String url = null;
                    try
                    {
                        url = getImageUrl(Context, url);
                    }
                    catch (NotSupportedException e)
                    {
                        await Context.Channel.SendMessageAsync(e.Message);
                        return;
                    }

                    FapiRequest body = new FapiRequest();
                    List<string> images = new List<string>();
                    images.Add(url);
                    body.images = images;

                    var response = service.ExecuteImageRequest("faceapp", text, body).Result;
                    response.Seek(0, System.IO.SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(response, "faceapp.png");
                }
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("facedetection", RunMode = RunMode.Async)]
        public async Task FaceDetection([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "detect_faces", url);
        }

        [Command("facemagik", RunMode = RunMode.Async)]
        public async Task FaceMagik(String text = "magik", int option = 0)
        {
                String url = null;
                try
                {
                    url = getImageUrl(Context, url);
                }
                catch (NotSupportedException e)
                {
                    await Context.Channel.SendMessageAsync(e.Message);
                    return;
                }

                FapiRequest body = new FapiRequest();
                List<string> images = new List<string>();
                images.Add(url);
                body.images = images;
                Arguments arguments = new Arguments();
                arguments.text = text;
                arguments.option = option;

                try
                {
                    var response = service.ExecuteImageRequest("facemagik", body).Result;
                    response.Seek(0, System.IO.SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(response, "facemagik.png");
                }
                catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
                {
                    HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                    if (caught.statusCode.Equals(429))
                    {
                        await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }

        [Command("faceoverlay", RunMode = RunMode.Async)]
        public async Task FaceOverlay(String firstImageUrl, String secondImageUrl)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(firstImageUrl))
            {
                string userId = firstImageUrl.Replace("<@!", "").Replace(">", "");
                foreach (var user in Context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        firstImageUrl = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }
            if (userIdCheck.IsMatch(secondImageUrl))
            {
                string userId = secondImageUrl.Replace("<@!", "").Replace(">", "");
                foreach (var user in Context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        secondImageUrl = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(firstImageUrl);
            images.Add(secondImageUrl);
            body.images = images;

            if ( !isSupportedImage(firstImageUrl) || !isSupportedImage(secondImageUrl))
            {
                throw new NotSupportedException("Provided image URLs are not supported or not images.");
            }

            try
            {
                var response = service.ExecuteImageRequest("faceoverlay", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "faceoverlay.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("faceswap", RunMode = RunMode.Async)]
        public async Task FaceSwap([Remainder] string url = null)
        {
            // TODO: Implementation here is incorrect
            throw new NotImplementedException();
            //await simpleImageRequest(Context, "faceswap", url);
        }

        [Command("gaben", RunMode = RunMode.Async)]
        public async Task Gaben([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "gaben", url);
        }

        [Command("gay", RunMode = RunMode.Async)]
        public async Task Gay([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "gay", url);
        }

        [Command("glitch", RunMode = RunMode.Async)]
        public async Task Glitch(int iterations = 10, int amount = 5)
        {
            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.iterations = iterations;
            arguments.amount = amount;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("glitch", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "glitch.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("glow", RunMode = RunMode.Async)]
        public async Task Glow(int amount = 12)
        {
            await imageRequestWithArguments(Context, "glow", amount.ToString());
        }

        [Command("god", RunMode = RunMode.Async)]
        public async Task God([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "god", url);
        }

        [Command("goldstar", RunMode = RunMode.Async)]
        public async Task Goldstar([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "goldstar", url);
        }

        [Command("grill", RunMode = RunMode.Async)]
        public async Task Grill([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "grill", text);
        }

        [Command("hacker", RunMode = RunMode.Async)]
        public async Task Hacker(int template, [Remainder] String text)
        {
            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.text = text;
            arguments.template = template;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("hacker", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "hacker.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("hawking", RunMode = RunMode.Async)]
        public async Task Hawking([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "hawking", url);
        }

        [Command("hypercam", RunMode = RunMode.Async)]
        public async Task HyperCam([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "hypercam", url);
        }

        [Command("idubbbz", RunMode = RunMode.Async)]
        public async Task IDubbbz([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "idubbbz", url);
        }

        [Command("ifunny", RunMode = RunMode.Async)]
        public async Task IFunny([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "ifunny", url);
        }

        [Command("imagetagparser", RunMode = RunMode.Async)]
        public async Task ImageTagParser([Remainder] String tagContent)
        {
            await simpleImageFromTextRequest(Context, "imagetagparser", tagContent);
        }

        [Command("isis", RunMode = RunMode.Async)]
        public async Task ISIS([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "isis", url);
        }

        [Command("israel", RunMode = RunMode.Async)]
        public async Task Israel([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "israel", url);
        }

        [Command("jack", RunMode = RunMode.Async)]
        public async Task Jack([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "jack", url);
        }

        [Command("jackoff", RunMode = RunMode.Async)]
        public async Task Jackoff([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "jackoff", url);
        }

        [Command("jesus", RunMode = RunMode.Async)]
        public async Task Jesus([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "jesus", url);
        }

        [Command("keemstar", RunMode = RunMode.Async)]
        public async Task Keemstar([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "keemstar", url);
        }

        [Command("keemstar2", RunMode = RunMode.Async)]
        public async Task Keemstar2([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "keemstar2", url);
        }

        [Command("kekistan", RunMode = RunMode.Async)]
        public async Task Kekistan([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "kekistan", url);
        }

        [Command("kirby", RunMode = RunMode.Async)]
        public async Task Kirby([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "kirby", url);
        }

        [Command("linus", RunMode = RunMode.Async)]
        public async Task Linus([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "linus", url);
        }

        [Command("logan", RunMode = RunMode.Async)]
        public async Task Logan([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "logan", url);
        }

        [Command("logout", RunMode = RunMode.Async)]
        public async Task Logout([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "logout", text);
        }

        [Command("magikscript", RunMode = RunMode.Async)]
        public async Task MagikScript()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("memorial", RunMode = RunMode.Async)]
        public async Task Memorial([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "memorial", text);
        }

        [Command("miranda", RunMode = RunMode.Async)]
        public async Task Miranda([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "miranda", url);
        }

        [Command("mistake", RunMode = RunMode.Async)]
        public async Task Mistake([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "mistake", url);
        }

        [Command("nooseguy", RunMode = RunMode.Async)]
        public async Task Nooseguy([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "nooseguy", url);
        }

        [Command("northkorea", RunMode = RunMode.Async)]
        public async Task NorthKorea([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "northkorea", url);
        }

        [Command("oldguy", RunMode = RunMode.Async)]
        public async Task OldGuy([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "oldguy", url);
        }

        [Command("owo", RunMode = RunMode.Async)]
        public async Task OwO([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "owo", url);
        }

        [Command("perfection", RunMode = RunMode.Async)]
        public async Task Perfection([Remainder] string url = null)
        {
            // TODO: Implementation here is incorrect
            throw new NotImplementedException();
            //await simpleImageRequest(Context, "perfection", url);
        }

        [Command("pistol", RunMode = RunMode.Async)]
        public async Task Pistol([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "pistol", url);
        }

        [Command("pixelate", RunMode = RunMode.Async)]
        public async Task Pixelate(int amount = 20)
        {
            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.amount = amount;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("pixelate", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "pixelate.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("pne", RunMode = RunMode.Async)]
        public async Task PNE(int option = 0)
        {
            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.option = option;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("pne", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "pne.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("pornhub", RunMode = RunMode.Async)]
        public async Task PornHub([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "pornhub", text);
        }

        [Command("portal", RunMode = RunMode.Async)]
        public async Task Portal([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "portal", url);
        }

        [Command("presidential", RunMode = RunMode.Async)]
        public async Task Presidential([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "presidential", text);
        }

        [Command("proxy", RunMode = RunMode.Async)]
        public async Task Proxy()
        {
            throw new NotImplementedException();
            // TODO: Investigate how this works and implement function
        }

        [Command("quote", RunMode = RunMode.Async)]
        public async Task Quote()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("racecard", RunMode = RunMode.Async)]
        public async Task Racecard([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "racecard", text);
        }

        [Command("realfact", RunMode = RunMode.Async)]
        public async Task Realfact([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "realfact", text);
        }

        [Command("recaptcha", RunMode = RunMode.Async)]
        public async Task ReCaptcha([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "recaptcha", text);
        }

        [Command("fapi_reminder", RunMode = RunMode.Async)]
        public async Task Reminder([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "reminder", text);
        }

        [Command("resize", RunMode = RunMode.Async)]
        public async Task Resize([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "resize", url);
        }

        [Command("respects", RunMode = RunMode.Async)]
        public async Task Respects([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "respects", url);
        }

        [Command("retro", RunMode = RunMode.Async)]
        public async Task Retro([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "retro", text);
        }

        [Command("rextester", RunMode = RunMode.Async)]
        public async Task RexTester(String language, [Remainder] String code)
        {
            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            arguments.text = code;
            arguments.language = language;
            body.args = arguments;

            try
            {
                var response = service.ExecuteTextRequest("rextester", body).Result;
                await Context.Channel.SendMessageAsync(response);
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("rtx", RunMode = RunMode.Async)]
        public async Task RTX(String firstImageUrl, String secondImageUrl)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(firstImageUrl))
            {
                string userId = firstImageUrl.Replace("<@!", "").Replace(">", "");
                foreach (var user in Context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        firstImageUrl = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }
            if (userIdCheck.IsMatch(secondImageUrl))
            {
                string userId = secondImageUrl.Replace("<@!", "").Replace(">", "");
                foreach (var user in Context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        secondImageUrl = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(firstImageUrl);
            images.Add(secondImageUrl);
            body.images = images;

            if (!isSupportedImage(firstImageUrl) || !isSupportedImage(secondImageUrl))
            {
                throw new NotSupportedException("Provided image URLs are not supported or not images.");
            }

            try
            {
                var response = service.ExecuteImageRequest("rtx", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "rtx.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("russia", RunMode = RunMode.Async)]
        public async Task Russia([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "russia", url);
        }

        [ProhibitBlacklistedServers]
        [Command("screenshot", RunMode = RunMode.Async)]
        public async Task Screenshot(String url, int wait = 0)
        {
            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            arguments.text = url;
            arguments.wait = wait;
            arguments.allowNSFW = Context.Guild.GetTextChannel(Context.Message.Channel.Id).IsNsfw;

            try
            {
                var response = service.ExecuteImageRequest("screenshot", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "screenshot.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("shit", RunMode = RunMode.Async)]
        public async Task Shit([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "shit", url);
        }

        [Command("shooting", RunMode = RunMode.Async)]
        public async Task Shooting([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "shooting", text);
        }

        [Command("shotgun", RunMode = RunMode.Async)]
        public async Task Shotgun([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "shotgun", url);
        }

        [Command("simpsonsdisabled", RunMode = RunMode.Async)]
        public async Task SimpsonsDisabled([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "simpsondisabled", text);
        }

        [Command("smg", RunMode = RunMode.Async)]
        public async Task SMG([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "smg", url);
        }

        [Command("snapchat", RunMode = RunMode.Async)]
        public async Task Snapchat(String filter, bool snow = false)
        {
            // TODO: Add validation for filter

            if (DateTime.Now.Month.Equals(12) && filter.Equals("christmas")) snow = true;

            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.text = filter;
            arguments.snow = snow;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("snapchat", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "snapchat.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("sonic", RunMode = RunMode.Async)]
        public async Task Sonic([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "sonic", text);
        }

        [Command("spain", RunMode = RunMode.Async)]
        public async Task Spain([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "spain", url);
        }

        [Command("starman", RunMode = RunMode.Async)]
        public async Task Starman([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "starman", url);
        }

        [Command("steamplaying", RunMode = RunMode.Async)]
        public async Task SteamPlaying([Remainder] String game)
        {
            try
            {
                FapiRequest body = new FapiRequest();
                Arguments arguments = new Arguments();
                arguments.text = game;
                body.args = arguments;

                var response = service.ExecuteTextRequest("steamplaying", body).Result;
                await Context.Channel.SendMessageAsync(response);
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("stock", RunMode = RunMode.Async)]
        public async Task Stock([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "stock", url);
        }

        [Command("supreme", RunMode = RunMode.Async)]
        public async Task Supreme([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "supreme", url);
        }

        [Command("thinking", RunMode = RunMode.Async)]
        public async Task Thinking(int level = 100)
        {
            String url = null;
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.level = level;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest("thinking", body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(response, "thinking.png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("thonkify", RunMode = RunMode.Async)]
        public async Task Thonkify([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "thonkify", text);
        }

        [Command("trans", RunMode = RunMode.Async)]
        public async Task Trans([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "trans", url);
        }

        [Command("trump", RunMode = RunMode.Async)]
        public async Task Trump([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "trump", url);
        }

        [Command("ugly", RunMode = RunMode.Async)]
        public async Task Ugly([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "ugly", url);
        }

        [Command("uk", RunMode = RunMode.Async)]
        public async Task UK([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "uk", url);
        }

        /*
         * urbandictionary not supported
         */

        [Command("urlify", RunMode = RunMode.Async)]
        public async Task URLify([Remainder] String url)
        {
            try
            {
                FapiRequest body = new FapiRequest();
                Arguments arguments = new Arguments();
                arguments.text = url;
                body.args = arguments;

                var response = service.ExecuteTextRequest("urlify", body).Result;
                await Context.Channel.SendMessageAsync(response);
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await Context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        [Command("ussr", RunMode = RunMode.Async)]
        public async Task USSR([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "ussr", url);
        }

        [Command("vending", RunMode = RunMode.Async)]
        public async Task Vending([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "vending", url);
        }

        [Command("watchmojo", RunMode = RunMode.Async)]
        public async Task WatchMojo([Remainder] String text)
        {
            await imageRequestWithArguments(Context, "watchmojo", text);
        }

        [Command("wheeze", RunMode = RunMode.Async)]
        public async Task Wheeze([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "wheeze", url);
        }

        [Command("wikihow", RunMode = RunMode.Async)]
        public async Task WikiHow()
        {
            throw new NotImplementedException();
            // TODO: Investigate how this works and implement function
        }

        [Command("wonka", RunMode = RunMode.Async)]
        public async Task Wonka([Remainder] String text)
        {
            await simpleImageFromTextRequest(Context, "wonka", text);
        }

        [Command("wth", RunMode = RunMode.Async)]
        public async Task WTH([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "wth", url);
        }

        [Command("yusuke", RunMode = RunMode.Async)]
        public async Task Yusuke([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "yusuke", url);
        }

        [Command("zoom", RunMode = RunMode.Async)]
        public async Task Zoom([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "zoom", url);
        }

        [Command("zuckerberg", RunMode = RunMode.Async)]
        public async Task Zuckerberg([Remainder] string url = null)
        {
            await simpleImageRequest(Context, "zuckerberg", url);
        }

        private string getNewestImageUrl(IMessage[] messages)
        {
            foreach (IMessage m in messages)
            {
                if (m.Type.Equals(MessageType.Default))
                {
                    if (m.Attachments != null && m.Attachments.Count > 0)
                    {
                        if (isSupportedImage(m.Attachments.First().Filename))
                        {
                            return m.Attachments.First().Url;
                        }
                    }
                    if (m.Content != null)
                    {
                        foreach (string s in m.Content.Split(' '))
                        {
                            if (isSupportedImage(s))
                            {
                                try
                                {
                                    Uri u = new Uri(s);
                                    return s;
                                }
                                catch (Exception e)
                                {
                                    // Do nothing, not a valid URI
                                }
                            }
                        }
                    }
                    if (m.Embeds != null)
                    {
                        foreach (Embed e in m.Embeds)
                        {
                            if (e.Image != null)
                            {
                                return e.Image.Value.Url;
                            }
                            if (e.Thumbnail != null)
                            {
                                return e.Thumbnail.Value.Url;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async Task simpleImageRequest(SocketCommandContext context, string command, string url = null)
        {
            try
            {
                url = getImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;

            try
            {
                var response = service.ExecuteImageRequest(command, body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await context.Channel.SendFileAsync(response, command + ".png");
            } catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        private async Task simpleImageFromTextRequest(SocketCommandContext context, string command, string text)
        {
            FapiRequest body = new FapiRequest();
            body.args = new Arguments();
            body.args.text = text;

            try
            {
                var response = service.ExecuteImageRequest(command, body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await context.Channel.SendFileAsync(response, command + ".png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }
  
        private async Task imageRequestWithArguments(SocketCommandContext context, string command, string text = null, string url = null)
        {
            try
            {
                url = getImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            Arguments arguments = new Arguments();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;
            arguments.text = text;
            body.args = arguments;

            try
            {
                var response = service.ExecuteImageRequest(command, body).Result;
                response.Seek(0, System.IO.SeekOrigin.Begin);
                await context.Channel.SendFileAsync(response, command + ".png");
            }
            catch (AggregateException ex) when (ex.InnerException is HttpStatusCodeException)
            {
                HttpStatusCodeException caught = (HttpStatusCodeException)ex.InnerException;
                if (caught.statusCode.Equals(429))
                {
                    await context.Channel.SendMessageAsync("fAPI daily quota has been exceeded. Unable to run command.");
                }
                else
                {
                    throw ex;
                }
            }
        }

        private string getImageUrl(SocketCommandContext context, string url)
        {
            if (url == null)
            {
                var messages = context.Channel.GetMessagesAsync(100, Discord.CacheMode.AllowDownload).FlattenAsync().Result.ToArray();
                messages.OrderBy(message => message.Timestamp);
                url = getNewestImageUrl(messages);
            }

            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(url))
            {
                string userId = url.Replace("<@!", "").Replace(">", "");
                foreach (var user in context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        url = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }

            if (!isSupportedImage(url))
            {
                throw new NotSupportedException("Invalid image specified or no image within 100 posts detected.");
            }

            return url;
        }

        private bool isSupportedImage(string url)
        {
            if (url.ToLower().EndsWith(".png") || url.ToLower().EndsWith(".gif") || url.ToLower().EndsWith(".jpeg") || url.ToLower().EndsWith(".jpg") || url.ToLower().EndsWith(".bmp"))
            {
                return true;
            }
            return false;
        }

        private String build4ChanPostString(Post post)
        {
            String result = "";

            result += "**" + post.user + " at " + post.timeAsString + "**\n";
            if (post.text != null && post.text.Length > 0) result += post.text + "\n";
            if (post.file != null && post.file.Length > 0) result += post.file + "\n";
            result += "\n";

            return result;
        }

        private String buildDuckDuckGoResultString(Result searchResult)
        {
            String result = "";

            result += "**" + searchResult.title + "**\n";
            result += searchResult.link + "\n";
            result += "\n";

            return result;
        }
    }
}
