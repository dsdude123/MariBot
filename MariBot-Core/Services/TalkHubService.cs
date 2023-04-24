using Discord;
using Discord.Commands;
using MariBot.Core.Models.TalkHub;
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
    public class TalkHubService
    {
        public async void GetTextToSpeech(SocketCommandContext context, string voice, string text)
        {
            try
            {
                SpeechRequest request = new SpeechRequest();
                request.VoiceName = voice;
                request.TextToSpeak = text;

                var talkHubResponse = SendRequest(request).Result;

                if (talkHubResponse.Status != RequestStatus.Failure)
                {
                    var acknowledgement = context.Channel.SendMessageAsync("Your request was accepted, I'll send an update when it is done.", messageReference: new MessageReference(context.Message.Id)).Result;

                    while (talkHubResponse.Status != RequestStatus.Done && talkHubResponse.Status != RequestStatus.Failure)
                    {
                        await Task.Delay(2000);
                        talkHubResponse = QueryStatus(talkHubResponse.RequestId).Result;
                    }

                    if (talkHubResponse.Status == RequestStatus.Done)
                    {
                        Stream speechData = GetVoiceData(talkHubResponse.RequestId).Result;
                        await context.Channel.DeleteMessageAsync(acknowledgement);
                        await context.Channel.SendFileAsync(speechData, $"{talkHubResponse.RequestId}.wav", messageReference: new MessageReference(context.Message.Id));
                    } else
                    {
                        throw new Exception(talkHubResponse.ErrorDetail.ToString());
                    }
                } else
                {
                    throw new Exception("TalkHub returned failure reponse at queue time");
                }
            } catch (Exception ex)
            {
                await context.Channel.SendMessageAsync($"Something went wrong. {ex.Message}", messageReference: new MessageReference(context.Message.Id));
            }
        }

        private async Task<SpeechResponse> SendRequest(SpeechRequest speechRequest)
        {
            HttpClient client = new HttpClient();
            var requestContent = new StringContent(JsonConvert.SerializeObject(speechRequest), Encoding.UTF8, "application/json");
            // TODO: Make domain configurable
            var response = client.PostAsync("http://tokyo-3-talkhub.nerv.jpn.com/api/SpeechRequest", requestContent).Result;

            return JsonConvert.DeserializeObject<SpeechResponse>(response.Content.ReadAsStringAsync().Result);
        }

        private async Task<SpeechResponse> QueryStatus(Guid guid)
        {
            HttpClient client = new HttpClient();
            string response = client.GetStringAsync($"http://tokyo-3-talkhub.nerv.jpn.com/api/SpeechRequest?guid={guid}").Result;

            return JsonConvert.DeserializeObject<SpeechResponse>(response);
        }

        private async Task<Stream> GetVoiceData(Guid guid)
        {
            HttpClient client = new HttpClient();
            return client.GetStreamAsync($"http://tokyo-3-talkhub.nerv.jpn.com/api/SpeechData?guid={guid}").Result;
        }
    }
}
