using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using MariBot.Models;
using MariBot.Services;

namespace MariBot.Modules
{
    [Group("webcam")]
    public class WebCamModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        [Command("help")]
        public Task help()
        {
            return ReplyAsync("https://dsdude123.github.io/MariBot/webcams.html");
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
