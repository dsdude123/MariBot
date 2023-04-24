using Discord.Commands;
using MariBot.Core.Models;
using MariBot.Core.Services;
using Newtonsoft.Json;

namespace MariBot.Core.Modules.Text
{
    [Group("webcam")]
    public class WebCamModule : ModuleBase<SocketCommandContext>
    {

        public ImageService ImageService { get; set; }

        public WebCamModule(ImageService imageService)
        {
            ImageService = imageService;
        }

        [Command("help")]
        public Task help()
        {
            return ReplyAsync("https://dsdude123.github.io/MariBot/webcams.html");
        }

        [Command]
        public async Task GetWebcam([Remainder] String webcamName)
        {
            if (webcamName.Equals("help"))
            {
                await help();
                return;
            }

            WebcamStore webcams = JsonConvert.DeserializeObject<WebcamStore>(File.ReadAllText(Environment.CurrentDirectory + "\\webcams.json"));
            Webcam foundCamera = null;
            foreach (Webcam i in webcams.cameras)
            {
                if (i.command.Equals(webcamName))
                {
                    foundCamera = i;
                }
            }

            if (foundCamera == null)
            {
                await ReplyAsync("Webcam not in database.");
                return;
            }

            var image = await ImageService.GetWebResource(foundCamera.url);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "camera.jpg");
        }
    }
}
