using Discord;
using Discord.Commands;
using StarBot.Models;
using StarBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Modules
{
    public class FapiModule : ModuleBase<SocketCommandContext>
    {
        public FapiService service { get; set; }

        [Command("e2p")]
        public async Task Edges2Porn([Remainder] string url)
        {
            if (url.Equals(null))
            {
                var messages = Context.Channel.GetMessagesAsync(100, Discord.CacheMode.AllowDownload).FlattenAsync().Result.ToArray();
                messages.OrderBy(message => message.Timestamp);
                url = getNewestImageUrl(messages);
            }

            if(!isSupportedImage(url))
            {
                await Context.Channel.SendMessageAsync("Invalid image specified or no image within 100 posts detected.");
                return;
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;

            HttpContent request = service.BuildImageRequest(body);
            var response = service.ExecuteImageRequest("edges2porn", request).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(response, "e2p.png");
        }

        private string getNewestImageUrl(IMessage[] messages)
        {
            foreach(IMessage m in messages)
            {
                if (m.Type.Equals(MessageType.Default))
                {
                    if(m.Attachments != null && m.Attachments.Count > 0)
                    {
                        if (isSupportedImage(m.Attachments.First().Filename))
                        {
                            return m.Attachments.First().Url;
                        }
                    }
                    if(m.Content != null)
                    {
                        foreach (string s in m.Content.Split(' '))
                        {
                            if(isSupportedImage(s))
                            {
                                try
                                {
                                    Uri u = new Uri(s);
                                    return s;
                                } catch (Exception e) { 
                                    // Do nothing, not a valid URI
                                }
                            }
                        }
                    }
                    if(m.Embeds != null)
                    {
                        foreach(Embed e in m.Embeds)
                        {
                            if(e.Image != null)
                            {
                                return e.Image.Value.Url;
                            }
                            if(e.Thumbnail != null)
                            {
                                return e.Thumbnail.Value.Url;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private bool isSupportedImage(string url)
        {
            if (url.ToLower().EndsWith(".png") || url.ToLower().EndsWith(".gif") || url.ToLower().EndsWith(".jpeg") || url.ToLower().EndsWith(".jpg") || url.ToLower().EndsWith(".bmp"))
            {
                return true;
            }
            return false;
        }
    }
}
