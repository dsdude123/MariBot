using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using StarBot.Services;

namespace StarBot.Modules
{
    [Group("webcam")]
    public class WebCamModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public Task help()
        {
            var output = "**Help file for webcam commands**\n\n";
            output += "**webcam redsquare** - Gets the current image of Red Square at UW Seattle.\n";
            output += "**webcam uwb** - Gets the current image of UW Bothell as viewed from the south garage.\n";
            output += "**webcam bellevue** - Gets the current image from the Bellevue towercam.\n";
            output += "**webcam seatac** - Gets the current image of Sea-Tac Airport.\n";
            output += "**webcam waterfront** - Gets the current image of the Seattle waterfront.\n";
            output += "**webcam seattle1** - Gets the current image of Seattle as viewed from KING 5's roofcam 1.\n";
            output += "**webcam seattle2** - Gets the current image of one of Seattle's stadiums. View occasionally changes.\n";
            output += "**webcam bozeman** - Gets the current image of a house in Bozeman, MT. Updates roughly once per hour.\n";
            output += "**webcam ephrata** - Gets the current image of the main highway in Ephrata, WA.\n";
            output += "**webcam spokane** - Gets the current image of a freeway in Spokane, WA.\n";
            output += "**webcam berkeley** - Gets the current image of Berkeley, CA.\n";
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            return ReplyAsync("", false, eb);
        }

        public PictureService PictureService { get; set; }

        [Command("redsquare"), Summary("Gets the current image of Red Square")]
        public async Task RedSquare()
        {
            var image = await PictureService.GetPictureAsync("http://www.washington.edu/cambots/camera1_l.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "redsquare.jpg");
        }

        [Command("uwb")]
        public async Task Uwb()
        {
            var image = await PictureService.GetPictureAsync("http://69.91.192.220/netcam.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "uwb.jpg");
        }

        [Command("bellevue")]
        public async Task Bellevue()
        {
            var image = await PictureService.GetPictureAsync("https://cdn.tegna-media.com/king/weather/bellevue.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bellevue.jpg");
        }

        [Command("seatac")]
        public async Task Seatac()
        {
            var image = await PictureService.GetPictureAsync("https://cdn.tegna-media.com/king/weather/seatac.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bellevue.jpg");
        }

        [Command("waterfront")]
        public async Task Waterfront()
        {
            var image = await PictureService.GetPictureAsync("https://cdn.tegna-media.com/king/weather/waterfront.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bellevue.jpg");
        }

        [Command("seattle1")]
        public async Task Seattle1()
        {
            var image = await PictureService.GetPictureAsync("https://cdn.tegna-media.com/king/weather/roofcam1nw500x281.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bellevue.jpg");
        }

        [Command("seattle2")]
        public async Task Seattle2()
        {
            var image = await PictureService.GetPictureAsync("https://cdn.tegna-media.com/king/weather/roofcam2ne500x281.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bellevue.jpg");
        }

        [Command("bozeman")]
        public async Task bozeman()
        {
            var image = await PictureService.GetPictureAsync("https://icons.wunderground.com/webcamramdisk/b/o/bozemanball/1/current.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bozeman.jpg");
        }

        [Command("ephrata")]
        public async Task ephrata()
        {
            var image = await PictureService.GetPictureAsync("http://images.wsdot.wa.gov/nc/028vc04659.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "bozeman.jpg");
        }

        [Command("spokane")]
        public async Task spokane()
        {
            var image = await PictureService.GetPictureAsync("http://images.wsdot.wa.gov/spokane/i90/CCTV004.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "spokane.jpg");
        }

        [Command("berkeley")]
        public async Task berkeley()
        {
            var image = await PictureService.GetPictureAsync("https://icons.wunderground.com//webcamramdisk/m/o/modululite/1/current.jpg");
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "berkeley.jpg");
        }
    }
}
