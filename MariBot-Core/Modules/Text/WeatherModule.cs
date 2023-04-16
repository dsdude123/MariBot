using System.Net;
using Discord;
using Discord.Commands;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Collection of commands relating to retrieving weather information
    /// </summary>
    public class WeatherModule : ModuleBase<SocketCommandContext>
    {
        private readonly WorkerManagerService workerManagerService;

        public WeatherModule(WorkerManagerService workerManagerService)
        {
            this.workerManagerService = workerManagerService;
        }

        [Command("radar")]
        public async Task radar(string stateCode = "WA")
        {
            string url;

            switch (stateCode.ToUpper())
            {
                case "WA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/tiw/animate.png";
                    break;
                case "NC":
                case "SC":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/clt/animate.png";
                    break;
                case "VA":
                case "MD":
                case "DC":
                case "WV":
                case "NJ":
                case "DE":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/shd/animate.png";
                    break;
                case "OR":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/rdm/animate.png";
                    break;
                case "CO":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/den/animate.png";
                    break;
                case "CA":
                case "CA-SOUTH":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bfl/animate.png";
                    break;
                case "NY":
                case "PA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bgm/animate.png";
                    break;
                case "ND":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bis/animate.png";
                    break;
                case "NH":
                case "VT":
                case "ME":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bml/animate.png";
                    break;
                case "TX":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/sat/animate.png"; // This one is problematic because Texas is too big
                    break;
                case "KY":
                case "TN":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bwg/animate.png";
                    break;
                case "MI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/cad/animate.png";
                    break;
                case "GA":
                case "AL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/csg/animate.png";
                    break;
                case "OH":
                case "IN":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/day/animate.png";
                    break;
                case "IA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/dsm/animate.png";
                    break;
                case "CT":
                case "MA":
                case "RI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/hfd/animate.png";
                    break;
                case "MO":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/jef/animate.png";
                    break;
                case "OK":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/law/animate.png";
                    break;
                case "NE":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lbf/animate.png";
                    break;
                case "AR":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lit/animate.png";
                    break;
                case "MT":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lwt/animate.png";
                    break;
                case "LA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/msy/animate.png";
                    break;
                case "ID":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/myl/animate.png";
                    break;
                case "FL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pie/animate.png";
                    break;
                case "SD":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pir/animate.png";
                    break;
                case "AZ":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/prc/animate.png";
                    break;
                case "UT":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pvu/animate.png";
                    break;
                case "WY":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/riw/animate.png";
                    break;
                case "NV":
                case "CA-NORTH":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/rno/animate.png";
                    break;
                case "NM":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/row/animate.png";
                    break;
                case "KS":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/sln/animate.png";
                    break;
                case "IL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/spi/animate.png";
                    break;
                case "MN":
                case "WI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/stc/animate.png";
                    break;
                case "MS":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/tvr/animate.png";
                    break;
                case "US":
                default:
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/none/usa/animate.png";
                    break;
            }
            if (stateCode.ToUpper().Equals("AK") || stateCode.ToUpper().Equals("HI"))
            {
                Context.Channel.SendMessageAsync("Sorry there is no radar map available, defaulting to contiguous United States");
            }

            var image = GetWebResource(url).Result;
            image.Seek(0, SeekOrigin.Begin);

            var conversionJob = new WorkerJob();
            conversionJob.Id = Guid.NewGuid();
            conversionJob.GuildId = Context.Guild.Id;
            conversionJob.ChannelId = Context.Channel.Id;
            conversionJob.MessageId = Context.Message.Id;
            conversionJob.SourceImage = image.ToArray();
            conversionJob.Command = Command.ConvertToDiscordFriendly;

            var queueResult = workerManagerService.EnqueueJob(conversionJob);

            Context.Channel.SendMessageAsync(queueResult, messageReference: new MessageReference(Context.Message.Id));
/*            MemoryStream outgoingImage = new MemoryStream();

            using (var baseImage = new MagickImageCollection(image))
            {
                using (var outputCollection = new MagickImageCollection())
                {
                    bool usedBaseOnce = false;
                    baseImage.Coalesce();
                    foreach (var frame in baseImage)
                    {
                        outputCollection.Add(new MagickImage(frame));
                    }
                    outputCollection.Write(outgoingImage, MagickFormat.Gif);
                }
            }
            outgoingImage.Seek(0, SeekOrigin.Begin);*/
            //Context.Channel.SendFileAsync(outgoingImage, "radar.gif");
        }

        private async Task<MemoryStream> GetWebResource(string url)
        {
            var request = WebRequest.CreateHttp(url);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";
            request.Method = "GET";
            using (var response = await request.GetResponseAsync())
            {
                MemoryStream stream = new MemoryStream();
                response.GetResponseStream().CopyTo(stream);
                return stream;
            }
        }
    }
}
