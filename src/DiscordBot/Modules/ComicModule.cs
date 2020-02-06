using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using StarBot.Services;
using HtmlAgilityPack;

namespace StarBot.Modules
{
    [Group("comic")]
    public class ComicModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }
        public HttpService HttpService { get; set; }

        public const int MAX_LOVENSTEIN = 5000;

        [Command("help")]
        public Task help()
        {
            var output = "**Help file for comic commands**\n\n";
            output += "**comic cyanide** - Gets a random Cyanide and Happiness comic.\n";
            output += "**comic xkcd** - Gets a random XKCD comic.\n";
            output += "**comic lovenstein** - Gets a random comic from Mr. Lovenstein.\n";
            output += "**comic jakelikesonions** - Gets a random comic from Jake Likes Onions.\n";
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            return ReplyAsync("", false, eb.Build());
        }

        [Command("cyanide")]
        public async Task Cyanide()
        {
            Stream html = await HttpService.GetHttpAsync("http://www.explosm.net/comics/random");
            HtmlDocument myDoc = new HtmlDocument();
            myDoc.Load(html);
            HtmlNode comic = myDoc.GetElementbyId("main-comic");
            string pictureURI = comic.Attributes["src"].Value;
            pictureURI = "http:" + pictureURI;
            var image = await PictureService.GetPictureAsync(pictureURI);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "randomcomic.jpg");
        }

        [Command("xkcd")]
        public async Task Xkcd()
        {
            Stream html = await HttpService.GetHttpAsync("https://c.xkcd.com/random/comic/");
            HtmlDocument myDoc = new HtmlDocument();
            myDoc.Load(html);
            HtmlNode comic = myDoc.GetElementbyId("comic").ChildNodes[1];
            string pictureURI = comic.Attributes["src"].Value;
            pictureURI = "http:" + pictureURI;
            var image = await PictureService.GetPictureAsync(pictureURI);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "randomcomic.jpg");
        }

        [Command("lovenstein")]
        public async Task Lovenstein()
        {
            HtmlDocument myDoc;
            while (true)
            {
                Random r = new Random();
                int rint = r.Next(1, MAX_LOVENSTEIN);
                Stream html = await HttpService.GetHttpAsync("http://www.mrlovenstein.com/comic/" + rint);
                myDoc = new HtmlDocument();
                myDoc.Load(html);
                if (myDoc.GetElementbyId("section_404") == null)
                {
                    break;
                }
            }

            HtmlNode comic = myDoc.GetElementbyId("comic_main_image");
            string pictureURI = comic.Attributes["src"].Value;
            pictureURI = "http://www.mrlovenstein.com" + pictureURI;
            var image = await PictureService.GetPictureAsync(pictureURI);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "randomcomic.jpg");
        }
    }
}
