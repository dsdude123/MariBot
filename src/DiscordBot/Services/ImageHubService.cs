using Discord;
using Discord.Commands;
using MariBot.Models.ImageHub;
using MariBot.Models.TalkHub;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class ImageHubService
    {
        public Dictionary<ulong, bool> HasActiveRequest;

        public ImageHubService()
        {
            HasActiveRequest = new Dictionary<ulong, bool>();
        }

        public async void ExecuteTextCommand(SocketCommandContext context, string provider, string prompt)
        {
            var user = context.User.Id;

            if (HasActiveRequest.ContainsKey(user) && HasActiveRequest[user])
            {
                await context.Channel.SendMessageAsync("You can only have one active request at a time. Wait for your previous request to complete and then try again.", messageReference: new MessageReference(context.Message.Id));
            } else
            {
                HasActiveRequest[user] = true;
                try
                {
                    ImageRequest request = new ImageRequest();
                    request.commandName = provider;
                    request.commandData = prompt;

                    var imageHubResponse = SendRequest(request).Result;

                    if (imageHubResponse.Status != RequestStatus.Failure)
                    {
                        var estimatedTime = GetEstimatedTimeString(imageHubResponse.EstimatedDeliveryTimeSeconds);
                        var acknowledgement = context.Channel.SendMessageAsync($"Your request was accepted, I'll send an update when it is done. The ETA is currently {estimatedTime}", messageReference: new MessageReference(context.Message.Id)).Result;

                        while (imageHubResponse.Status != RequestStatus.Done && imageHubResponse.Status != RequestStatus.Failure)
                        {
                            await Task.Delay(2000);
                            imageHubResponse = QueryStatus(imageHubResponse.RequestId).Result;
                        }

                        HasActiveRequest[user] = false;

                        if (imageHubResponse.Status == RequestStatus.Done)
                        {
                            Stream imageData = GetImageData(imageHubResponse.RequestId).Result;
                            await context.Channel.DeleteMessageAsync(acknowledgement);
                            await context.Channel.SendFileAsync(imageData, $"{imageHubResponse.RequestId}.png", messageReference: new MessageReference(context.Message.Id));
                        }
                        else
                        {
                            throw new Exception(imageHubResponse.ErrorDetail.ToString());
                        }
                    }
                    else
                    {
                        throw new Exception("ImageHub returned failure reponse at queue time");
                    }
                } catch (Exception ex)
                {
                    HasActiveRequest[user] = false;
                    var exMessage = ex.Message;
                    if (ex is AggregateException)
                    {
                        exMessage = ex.InnerException.Message;
                    }
                    await context.Channel.SendMessageAsync($"Something went wrong: {exMessage}", messageReference: new MessageReference(context.Message.Id));
                }
            }
        }

        private async Task<ImageResponse> SendRequest(ImageRequest ImageRequest)
        {
            HttpClient client = new HttpClient();
            var requestContent = new StringContent(JsonConvert.SerializeObject(ImageRequest), Encoding.UTF8, "application/json");
            // TODO: Make domain configurable
            var response = client.PostAsync("http://nerv.jpn.com:8090/api/ImageRequest", requestContent).Result;
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<ImageResponse>(response.Content.ReadAsStringAsync().Result);
        }

        private async Task<ImageResponse> QueryStatus(Guid guid)
        {
            HttpClient client = new HttpClient();
            string response = client.GetStringAsync($"http://nerv.jpn.com:8090/api/ImageRequest?guid={guid}").Result;

            return JsonConvert.DeserializeObject<ImageResponse>(response);
        }

        private async Task<Stream> GetImageData(Guid guid)
        {
            HttpClient client = new HttpClient();
            return client.GetStreamAsync($"http://nerv.jpn.com:8090/api/ImageData?guid={guid}").Result;
        }

        private string GetEstimatedTimeString(uint seconds)
        {
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            var remainder = (seconds % 3600) % 60;

            var result = "";
            if (hours > 0) result += $"{hours} hours ";
            if (minutes > 0 || hours > 0) result += $"{minutes} minutes ";
            result += $"{remainder} seconds";

            return result;
        }
    }
}
