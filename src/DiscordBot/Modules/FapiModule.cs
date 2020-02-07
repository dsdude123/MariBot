using Discord;
using Discord.Commands;
using StarBot.Models;
using StarBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarBot.Modules
{
    public class FapiModule : ModuleBase<SocketCommandContext>
    {
        public FapiService service { get; set; }

        [Command("e2e")]
        public async Task Edges2Emojis(string url = null)
        {
            try
            {
                url = getImageUrl(Context, url);
            } 
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;

            var response = service.ExecuteImageRequest("edges2emojis", body).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(response, "e2e.png");
        }

        [RequireNsfw]
        [Command("e2p")]
        public async Task Edges2Porn(string url = null)
        {
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;

            var response = service.ExecuteImageRequest("edges2porn", body).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(response, "e2p.png");
        }

        [RequireNsfw]
        [Command("e2pg")]
        public async Task Edges2PornGif(string url = null)
        {
            try
            {
                url = getImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            FapiRequest body = new FapiRequest();
            List<string> images = new List<string>();
            images.Add(url);
            body.images = images;

            var response = service.ExecuteImageRequest("edges2porn_gif", body).Result;
            response.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(response, "e2p.gif");
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

        private string getImageUrl(SocketCommandContext context, string url)
        {
            if (url == null)
            {
                var messages = context.Channel.GetMessagesAsync(100, Discord.CacheMode.AllowDownload).FlattenAsync().Result.ToArray();
                messages.OrderBy(message => message.Timestamp);
                url = getNewestImageUrl(messages);
            }

            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(url))
            {
                string userId = url.Replace("<@!", "").Replace(">", "");
                foreach (var user in context.Guild.Users.ToArray())
                {
                    if (user.Id.ToString().Equals(userId))
                    {
                        url = user.GetAvatarUrl().Split('?')[0];
                    }
                }
            }


            if (!isSupportedImage(url))
            {
                throw new NotSupportedException("Invalid image specified or no image within 100 posts detected.");
            }

            return url;
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
