using Discord;
using Discord.Commands;
using MariBot.Models;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    public class FapiModule : ModuleBase<SocketCommandContext>
    {
        public FapiService service { get; set; }

        //[Command("4chan")]
        public Task fourchan()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("9gag", RunMode = RunMode.Async)]
        public async Task ninegag(string url = null)
        {
            await simpleImageRequest(Context, "9gag", url);
        }

        [Command("adidas", RunMode = RunMode.Async)]
        public async Task Adidas(string url = null)
        {
            await simpleImageRequest(Context, "adidas", url);
        }

        [Command("adw", RunMode = RunMode.Async)]
        public async Task ADW(string url = null)
        {
            await simpleImageRequest(Context, "adw", url);
        }

        [Command("ajit", RunMode = RunMode.Async)]
        public async Task Ajit(string url = null)
        {
            await simpleImageRequest(Context, "ajit", url);
        }

        [Command("america", RunMode = RunMode.Async)]
        public async Task America(string url = null)
        {
            await simpleImageRequest(Context, "america", url);
        }

        [Command("analysis", RunMode = RunMode.Async)]
        public async Task Analysis(string url = null)
        {
            await simpleImageRequest(Context, "analysis", url);
        }

        [Command("austin", RunMode = RunMode.Async)]
        public async Task Austin(string url = null)
        {
            await simpleImageRequest(Context, "austin", url);
        }

        [Command("autism", RunMode = RunMode.Async)]
        public async Task Autism(string url = null)
        {
            await simpleImageRequest(Context, "autism", url);
        }

        [Command("bandicam", RunMode = RunMode.Async)]
        public async Task Bandicam(string url = null)
        {
            await simpleImageRequest(Context, "bandicam", url);
        }

        [Command("bernie", RunMode = RunMode.Async)]
        public async Task Bernie(string url = null)
        {
            await simpleImageRequest(Context, "bernie", url);
        }

        [Command("binoculars", RunMode = RunMode.Async)]
        public async Task Binoculars(string url = null)
        {
            await simpleImageRequest(Context, "binoculars", url);
        }

        [Command("blackify", RunMode = RunMode.Async)]
        public async Task Blackify(string url = null)
        {
            try
            {
                await simpleImageRequest(Context, "blackify", url);
            }
            catch (AggregateException e)
            {
                if (e.InnerException.Message.Equals("Response status code does not indicate success: 400 (Bad Request)."))
                {
                    await Context.Channel.SendMessageAsync("400 Bad Request. fAPI may have not been able to detect a face in the picture");
                }
                throw e;
            }
        }

        [Command("blackpanther", RunMode = RunMode.Async)]
        public async Task BlackPanther(string url = null)
        {
            await simpleImageRequest(Context, "blackpanther", url);
        }

        [Command("bobross", RunMode = RunMode.Async)]
        public async Task BobRoss(string url = null)
        {
            await simpleImageRequest(Context, "bobross", url);
        }

        //[Command("buzzfeed", RunMode = RunMode.Async)]
        public async Task BuzzFeed(string url = null)
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("cmm", RunMode = RunMode.Async)]
        public async Task ChangeMyMind([Remainder] string text)
        {
            FapiRequest body = new FapiRequest();
            body.args = new Arguments();
            body.args.text = text;

            var response = service.ExecuteImageRequest("cmm", body).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(response, "cmm.png");
        }

        [Command("composite", RunMode = RunMode.Async)]
        public async Task Composite(string url = null)
        {
            await simpleImageRequest(Context, "composite", url);
        }

        [Command("condom", RunMode = RunMode.Async)]
        public async Task Condom(string url = null)
        {
            await simpleImageRequest(Context, "condom", url);
        }

        //[Command("consent", RunMode = RunMode.Async)]
        public async Task Consent()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("coolguy", RunMode = RunMode.Async)]
        public async Task Coolguy(string url = null)
        {
            await simpleImageRequest(Context, "coolguy", url);
        }

        //[Command("days", RunMode = RunMode.Async)]
        public async Task Days()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("deepfry", RunMode = RunMode.Async)]
        public async Task Deepfry()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("depression", RunMode = RunMode.Async)]
        public async Task Depression(string url = null)
        {
            await simpleImageRequest(Context, "depression", url);
        }

        [Command("disabled", RunMode = RunMode.Async)]
        public async Task Disabled(string url = null)
        {
            await simpleImageRequest(Context, "disabled", url);
        }

        [Command("dork", RunMode = RunMode.Async)]
        public async Task Dork(string url = null)
        {
            await simpleImageRequest(Context, "dork", url);
        }

        //[Command("duckduckgo", RunMode = RunMode.Async)]
        public async Task DuckDuckGo()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("duckduckgoimages", RunMode = RunMode.Async)]
        public async Task DuckDuckGoImages()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("edges", RunMode = RunMode.Async)]
        public async Task Edges(string url = null)
        {
            await simpleImageRequest(Context, "edges", url);
        }

        [Command("e2e", RunMode = RunMode.Async)]
        public async Task Edges2Emojis(string url = null)
        {
            await simpleImageRequest(Context, "edges2emojis", url);
        }

        [RequireNsfw]
        [Command("e2p", RunMode = RunMode.Async)]
        public async Task Edges2Porn(string url = null)
        {
            await simpleImageRequest(Context, "edges2porn", url);
        }

        [RequireNsfw]
        [Command("e2pg", RunMode = RunMode.Async)]
        public async Task Edges2PornGif(string url = null)
        {
            await simpleImageRequest(Context, "edges2porn_gif", url);
        }

        //[Command("emojify", RunMode = RunMode.Async)]
        public async Task Emojify()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("emojimosaic", RunMode = RunMode.Async)]
        public async Task EmojiMosaic()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("eval", RunMode = RunMode.Async)]
        public async Task Eval()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("EvalMagik", RunMode = RunMode.Async)]
        public async Task EvalMagik()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("excuse", RunMode = RunMode.Async)]
        public async Task Excuse(string url = null)
        {
            await simpleImageRequest(Context, "excuse", url);
        }

        //[Command("eyes", RunMode = RunMode.Async)]
        public async Task Eyes()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("faceapp", RunMode = RunMode.Async)]
        public async Task FaceApp()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("facedetection", RunMode = RunMode.Async)]
        public async Task FaceDetection(string url = null)
        {
            await simpleImageRequest(Context, "detect_faces", url);
        }

        //[Command("facemagik", RunMode = RunMode.Async)]
        public async Task FaceMagik()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("faceoverlay", RunMode = RunMode.Async)]
        public async Task FaceOverlay()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("faceswap", RunMode = RunMode.Async)]
        public async Task FaceSwap(string url = null)
        {
            await simpleImageRequest(Context, "faceswap", url);
        }

        [Command("gaben", RunMode = RunMode.Async)]
        public async Task Gaben(string url = null)
        {
            await simpleImageRequest(Context, "gaben", url);
        }

        [Command("gay", RunMode = RunMode.Async)]
        public async Task Gay(string url = null)
        {
            await simpleImageRequest(Context, "gay", url);
        }

        //[Command("glitch", RunMode = RunMode.Async)]
        public async Task Glitch()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("glow", RunMode = RunMode.Async)]
        public async Task Glow()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("god", RunMode = RunMode.Async)]
        public async Task God(string url = null)
        {
            await simpleImageRequest(Context, "god", url);
        }

        [Command("goldstar", RunMode = RunMode.Async)]
        public async Task Goldstar(string url = null)
        {
            await simpleImageRequest(Context, "goldstar", url);
        }

        //[Command("grill", RunMode = RunMode.Async)]
        public async Task Grill()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("hacker", RunMode = RunMode.Async)]
        public async Task Hacker()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("hawking", RunMode = RunMode.Async)]
        public async Task Hawking(string url = null)
        {
            await simpleImageRequest(Context, "hawking", url);
        }

        [Command("hypercam", RunMode = RunMode.Async)]
        public async Task HyperCam(string url = null)
        {
            await simpleImageRequest(Context, "hypercam", url);
        }

        [Command("idubbbz", RunMode = RunMode.Async)]
        public async Task IDubbbz(string url = null)
        {
            await simpleImageRequest(Context, "idubbbz", url);
        }

        [Command("ifunny", RunMode = RunMode.Async)]
        public async Task IFunny(string url = null)
        {
            await simpleImageRequest(Context, "ifunny", url);
        }

        //[Command("imagetagparser", RunMode = RunMode.Async)]
        public async Task ImageTagParser()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("isis", RunMode = RunMode.Async)]
        public async Task ISIS(string url = null)
        {
            await simpleImageRequest(Context, "isis", url);
        }

        [Command("israel", RunMode = RunMode.Async)]
        public async Task Israel(string url = null)
        {
            await simpleImageRequest(Context, "israel", url);
        }

        [Command("jack", RunMode = RunMode.Async)]
        public async Task Jack(string url = null)
        {
            await simpleImageRequest(Context, "jack", url);
        }

        [Command("jackoff", RunMode = RunMode.Async)]
        public async Task Jackoff(string url = null)
        {
            await simpleImageRequest(Context, "jackoff", url);
        }

        [Command("jesus", RunMode = RunMode.Async)]
        public async Task Jesus(string url = null)
        {
            await simpleImageRequest(Context, "jesus", url);
        }

        [Command("keemstar", RunMode = RunMode.Async)]
        public async Task Keemstar(string url = null)
        {
            await simpleImageRequest(Context, "keemstar", url);
        }

        [Command("keemstar2", RunMode = RunMode.Async)]
        public async Task Keemstar2(string url = null)
        {
            await simpleImageRequest(Context, "keemstar2", url);
        }

        [Command("kekistan", RunMode = RunMode.Async)]
        public async Task Kekistan(string url = null)
        {
            await simpleImageRequest(Context, "kekistan", url);
        }

        [Command("kirby", RunMode = RunMode.Async)]
        public async Task Kirby(string url = null)
        {
            await simpleImageRequest(Context, "kirby", url);
        }

        [Command("linus", RunMode = RunMode.Async)]
        public async Task Linus(string url = null)
        {
            await simpleImageRequest(Context, "linus", url);
        }

        [Command("logan", RunMode = RunMode.Async)]
        public async Task Logan(string url = null)
        {
            await simpleImageRequest(Context, "logan", url);
        }

        //[Command("logout", RunMode = RunMode.Async)]
        public async Task Logout()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("magikscript", RunMode = RunMode.Async)]
        public async Task MagikScript()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("memorial", RunMode = RunMode.Async)]
        public async Task Memorial()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("miranda", RunMode = RunMode.Async)]
        public async Task Miranda(string url = null)
        {
            await simpleImageRequest(Context, "miranda", url);
        }

        [Command("mistake", RunMode = RunMode.Async)]
        public async Task Mistake(string url = null)
        {
            await simpleImageRequest(Context, "mistake", url);
        }

        [Command("nooseguy", RunMode = RunMode.Async)]
        public async Task Nooseguy(string url = null)
        {
            await simpleImageRequest(Context, "nooseguy", url);
        }

        [Command("northkorea", RunMode = RunMode.Async)]
        public async Task NorthKorea(string url = null)
        {
            await simpleImageRequest(Context, "northkorea", url);
        }

        [Command("oldguy", RunMode = RunMode.Async)]
        public async Task OldGuy(string url = null)
        {
            await simpleImageRequest(Context, "oldguy", url);
        }

        [Command("owo", RunMode = RunMode.Async)]
        public async Task OwO(string url = null)
        {
            await simpleImageRequest(Context, "owo", url);
        }

        [Command("perfection", RunMode = RunMode.Async)]
        public async Task Perfection(string url = null)
        {
            await simpleImageRequest(Context, "perfection", url);
        }

        [Command("pistol", RunMode = RunMode.Async)]
        public async Task Pistol(string url = null)
        {
            await simpleImageRequest(Context, "pistol", url);
        }

        //[Command("pixelate", RunMode = RunMode.Async)]
        public async Task Pixelate()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("pne", RunMode = RunMode.Async)]
        public async Task PNE()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("pornhub", RunMode = RunMode.Async)]
        public async Task PornHub()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("portal", RunMode = RunMode.Async)]
        public async Task Portal(string url = null)
        {
            await simpleImageRequest(Context, "portal", url);
        }

        //[Command("presidential", RunMode = RunMode.Async)]
        public async Task Presidential()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("proxy", RunMode = RunMode.Async)]
        public async Task Proxy()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("quote", RunMode = RunMode.Async)]
        public async Task Quote()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("racecard", RunMode = RunMode.Async)]
        public async Task Racecard()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("realfact", RunMode = RunMode.Async)]
        public async Task Realfact()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("recaptcha", RunMode = RunMode.Async)]
        public async Task ReCaptcha()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("reminder", RunMode = RunMode.Async)]
        public async Task Reminder()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("resize", RunMode = RunMode.Async)]
        public async Task Resize(string url = null)
        {
            await simpleImageRequest(Context, "resize", url);
        }

        [Command("respects", RunMode = RunMode.Async)]
        public async Task Respects(string url = null)
        {
            await simpleImageRequest(Context, "respects", url);
        }

        //[Command("retro", RunMode = RunMode.Async)]
        public async Task Retro()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("rextester", RunMode = RunMode.Async)]
        public async Task RexTester()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("rtx", RunMode = RunMode.Async)]
        public async Task RTX(string url = null)
        {
            throw new NotImplementedException();
            // TODO: Implement function (needs 2 images)
        }

        [Command("russia", RunMode = RunMode.Async)]
        public async Task Russia(string url = null)
        {
            await simpleImageRequest(Context, "russia", url);
        }

        //[Command("screenshot", RunMode = RunMode.Async)]
        public async Task Screenshot()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("shit", RunMode = RunMode.Async)]
        public async Task Shit(string url = null)
        {
            await simpleImageRequest(Context, "shit", url);
        }

        //[Command("shooting", RunMode = RunMode.Async)]
        public async Task Shooting()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("shotgun", RunMode = RunMode.Async)]
        public async Task Shotgun(string url = null)
        {
            await simpleImageRequest(Context, "shotgun", url);
        }

        //[Command("simpsonsdisabled", RunMode = RunMode.Async)]
        public async Task SimpsonsDisabled()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("smg", RunMode = RunMode.Async)]
        public async Task SMG(string url = null)
        {
            await simpleImageRequest(Context, "smg", url);
        }

        //[Command("snapchat", RunMode = RunMode.Async)]
        public async Task Snapchat()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("sonic", RunMode = RunMode.Async)]
        public async Task Sonic()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("spain", RunMode = RunMode.Async)]
        public async Task Spain(string url = null)
        {
            await simpleImageRequest(Context, "spain", url);
        }

        [Command("starman", RunMode = RunMode.Async)]
        public async Task Starman(string url = null)
        {
            await simpleImageRequest(Context, "starman", url);
        }

        //[Command("stats", RunMode = RunMode.Async)]
        public async Task Stats()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("steamplaying", RunMode = RunMode.Async)]
        public async Task SteamPlaying()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("stock", RunMode = RunMode.Async)]
        public async Task Stock(string url = null)
        {
            await simpleImageRequest(Context, "stock", url);
        }

        [Command("supreme", RunMode = RunMode.Async)]
        public async Task Supreme(string url = null)
        {
            await simpleImageRequest(Context, "supreme", url);
        }

        //[Command("thinking", RunMode = RunMode.Async)]
        public async Task Thinking()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("thonkify", RunMode = RunMode.Async)]
        public async Task Thonkify()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("trans", RunMode = RunMode.Async)]
        public async Task Trans(string url = null)
        {
            await simpleImageRequest(Context, "trans", url);
        }

        [Command("trump", RunMode = RunMode.Async)]
        public async Task Trump(string url = null)
        {
            await simpleImageRequest(Context, "trump", url);
        }

        [Command("ugly", RunMode = RunMode.Async)]
        public async Task Ugly(string url = null)
        {
            await simpleImageRequest(Context, "ugly", url);
        }

        [Command("uk", RunMode = RunMode.Async)]
        public async Task UK(string url = null)
        {
            await simpleImageRequest(Context, "uk", url);
        }

        /*
         * urbandictionary not supported
         */

        //[Command("urlify", RunMode = RunMode.Async)]
        public async Task URLify()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("ussr", RunMode = RunMode.Async)]
        public async Task USSR(string url = null)
        {
            await simpleImageRequest(Context, "ussr", url);
        }

        [Command("vending", RunMode = RunMode.Async)]
        public async Task Vending(string url = null)
        {
            await simpleImageRequest(Context, "vending", url);
        }

        //[Command("watchmojo", RunMode = RunMode.Async)]
        public async Task WatchMojo()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("wheeze", RunMode = RunMode.Async)]
        public async Task Wheeze(string url = null)
        {
            await simpleImageRequest(Context, "wheeze", url);
        }

        //[Command("wikihow", RunMode = RunMode.Async)]
        public async Task WikiHow()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        //[Command("wonka", RunMode = RunMode.Async)]
        public async Task Wonka()
        {
            throw new NotImplementedException();
            // TODO: Implement function
        }

        [Command("wth", RunMode = RunMode.Async)]
        public async Task WTH(string url = null)
        {
            await simpleImageRequest(Context, "wth", url);
        }

        [Command("yusuke", RunMode = RunMode.Async)]
        public async Task Yusuke(string url = null)
        {
            await simpleImageRequest(Context, "yusuke", url);
        }

        [Command("zoom", RunMode = RunMode.Async)]
        public async Task Zoom(string url = null)
        {
            await simpleImageRequest(Context, "zoom", url);
        }

        [Command("zuckerberg", RunMode = RunMode.Async)]
        public async Task Zuckerberg(string url = null)
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

            var response = service.ExecuteImageRequest(command, body).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await context.Channel.SendFileAsync(response, command + ".png");
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
    }
}
