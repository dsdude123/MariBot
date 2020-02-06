using Discord.Commands;
using StarBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Modules
{
    public class NsfwModule : ModuleBase<SocketCommandContext>
    {
        public Rule34Service rule34Service { get; set; }
        public PictureService pictureService { get; set; }

        [Command("r34")]
        public async Task r34([Remainder] string tags)
        {
            if(!Context.Channel.IsNsfw)
            {
                await Context.Channel.SendMessageAsync("You must be in an NSFW channel to run this command.");
                return;
            }
            List<string> images = rule34Service.GetRandomPage(tags).Result;
            if(images.Count < 1)
            {
                await Context.Channel.SendMessageAsync("No results were found.");
                return;
            }
            Random rand = new Random();
            int randomSelection = rand.Next(0, images.Count - 1);
            string selectedImage = images[randomSelection];

            var image = pictureService.GetPictureAsync(selectedImage).Result;
            await Context.Channel.SendFileAsync(image, "nsfw.jpg");
        }
    }
}
