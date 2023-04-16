using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    [Group("comic")]
    public class ComicModule : ModuleBase<SocketCommandContext>
    {
        private ImageService imageService { get; set; }

        public const int MAX_LOVENSTEIN = 5000;

        public ComicModule(ImageService imageService)
        {
            this.imageService = imageService;
        }

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
            Stream html = await imageService.GetWebResource("http://www.explosm.net/comics/random");
            HtmlDocument myDoc = new HtmlDocument();
            myDoc.Load(html);
            HtmlNode comic = myDoc.GetElementbyId("main-comic");
            string pictureURI = comic.Attributes["src"].Value;
            pictureURI = "http:" + pictureURI;
            var image = await imageService.GetWebResource(pictureURI);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "randomcomic.jpg");
        }

        [Command("xkcd")]
        public async Task Xkcd()
        {
            Stream html = await imageService.GetWebResource("https://c.xkcd.com/random/comic/");
            HtmlDocument myDoc = new HtmlDocument();
            myDoc.Load(html);
            HtmlNode comic = myDoc.GetElementbyId("comic").ChildNodes[1];
            string pictureURI = comic.Attributes["src"].Value;
            pictureURI = "http:" + pictureURI;
            var image = await imageService.GetWebResource(pictureURI);
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
                Stream html = await imageService.GetWebResource("http://www.mrlovenstein.com/comic/" + rint);
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
            var image = await imageService.GetWebResource(pictureURI);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "randomcomic.jpg");
        }
    }
}
