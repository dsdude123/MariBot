using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using StarBot.Models;
using StarBot.Services;

namespace StarBot.Modules
{
    [Group("webcam")]
    public class WebCamModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        [Command("help")]
        public async Task help()
        {
            WebcamStore webcams = JsonConvert.DeserializeObject<WebcamStore>(File.ReadAllText(Environment.CurrentDirectory + "\\webcams.json"));
            while (webcams.cameras.Count > 0)
            {
                var output = "**Webcam Listing**\n\n Use webcam + camera name to select. Example: z webcam seattle\n\n";
                List<Webcam> toRemove = new List<Webcam>();
                foreach(Webcam i in webcams.cameras)
                {
                    String newText = "**" + i.command + "** - " + i.description + "\n";
                    if ((output + newText).Length >= 2000)
                    {
                        break;
                    }
                    output += newText;
                    toRemove.Add(i);
                }
                foreach(Webcam i in toRemove)
                {
                    webcams.cameras.Remove(i);
                }

                var eb = new EmbedBuilder();
                eb.WithDescription(output);
                await ReplyAsync("", false, eb.Build());
                System.Threading.Thread.Sleep(1000);
            }
        }

        [Command]
        public async Task GetWebcam([Remainder] String webcamName)
        {
            if(webcamName.Equals("help"))
            {
                await help();
                return;
            }

            WebcamStore webcams = JsonConvert.DeserializeObject<WebcamStore>(File.ReadAllText(Environment.CurrentDirectory + "\\webcams.json"));
            Webcam foundCamera = null;
            foreach(Webcam i in webcams.cameras)
            {
                if(i.command.Equals(webcamName))
                {
                    foundCamera = i;
                }
            }

            if(foundCamera == null)
            {
                await ReplyAsync("Webcam not in database.");
                return;
            }

            var image = await PictureService.GetPictureAsync(foundCamera.url);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "camera.jpg");
        }
    }
}
