using System.Net;
using System.Text.RegularExpressions;
using CSharpMath.SkiaSharp;
using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using SkiaSharp;

namespace MariBot.Core.Services
{
    public class ImageService
    {
        private const string GiphyDomain = "giphy.com";
        private const string ImgurDomain = "imgur.com";
        private const string TenorDomain = "tenor.com";

        private static readonly string[] SupportedGifDomains = new[] { GiphyDomain, ImgurDomain, TenorDomain };

        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";

        /// <summary>
        /// Renders a text string into an image using LaTeX
        /// </summary>
        /// <param name="textToRender">Latex formatted string</param>
        /// <returns>Stream containing a PNG</returns>
        public Stream GenerateLatexImage(string textToRender)
        {
            // Render the LaTeX
            var painter = new TextPainter
            {
                LaTeX = textToRender,
                FontSize = 48f
            };

            var latexStream = painter.DrawAsStream(format: SKEncodedImageFormat.Png);
            var skBitmap = SKBitmap.Decode(latexStream);

            // Setup the background to be white
            var skImageInfo = new SKImageInfo(skBitmap.Width, skBitmap.Height);
            var skSurface = SKSurface.Create(skImageInfo);
            var skCanvas = skSurface.Canvas;

            skCanvas.Clear(SKColors.White);

            var skPaint = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.High
            };

            skCanvas.DrawBitmap(skBitmap, skImageInfo.Rect, skPaint);
            skCanvas.Flush();

            // Send the final result
            var skResult = skSurface.Snapshot();
            var skData = skResult.Encode();
            return skData.AsStream();
        }

        /// <summary>
        /// Gets direct GIF link from a Giphy GIF
        /// </summary>
        /// <param name="url">Giphy URL</param>
        /// <returns>Direct link to the GIF</returns>
        public string GetGiphyGif(string url)
        {
            var web = new HtmlWeb
            {
                UserAgent = UserAgent
            };
            var document = web.Load(url);
            var contentUrl = new Uri(document.DocumentNode.SelectSingleNode("//meta[@property='og:image']").Attributes["content"].Value);
            return contentUrl.AbsoluteUri;
        }

        /// <summary>
        /// Gets direct image link from a Imgur file
        /// </summary>
        /// <param name="url">Imgur URL</param>
        /// <returns>Direct link to the image</returns>
        public string GetImgurGif(string url)
        {
            var web = new HtmlWeb();
            web.UserAgent = UserAgent;
            var document = web.Load(url);
            var contentUrl = new Uri(document.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value);
            return contentUrl.AbsoluteUri;
        }

        /// <summary>
        /// Gets direct GIF link from a Tenor GIF
        /// </summary>
        /// <param name="url">Tenor URL</param>
        /// <returns>Direct link to the GIF</returns>
        public string GetTenorGif(string url)
        {
            var web = new HtmlWeb
            {
                UserAgent = UserAgent
            };
            var document = web.Load(url);
            var contentUrl = new Uri(document.DocumentNode.SelectSingleNode("//meta[@itemprop='contentUrl']").Attributes["content"].Value);
            return contentUrl.AbsoluteUri;
        }

        /// <summary>
        /// Downloads a file from the internet.
        /// </summary>
        /// <param name="url">HTTP URL</param>
        /// <returns>Stream</returns>
        public async Task<Stream> GetWebResource(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };
            request.Headers.Add("User-Agent", UserAgent);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        /// <summary>
        /// Finds the most recently sent image from a list of messages.
        /// </summary>
        /// <param name="context">Discord context</param>
        /// <returns>String URL to the image</returns>
        public async Task<string> GetImageUrl(SocketCommandContext context)
        {
            // Check if the context is a mention indicating we want the users avatar
            if (!string.IsNullOrWhiteSpace(context.Message.Content))
            {
                foreach (var part in context.Message.Content.Split(' '))
                {
                    var userIdCheck = new Regex(@"<@[0-9]+>", RegexOptions.Compiled);
                    if (!userIdCheck.IsMatch(part)) continue;
                    var userId = part.Replace("<@", "").Replace(">", "");
                    context.Guild.DownloadUsersAsync().Wait();
                    foreach (var user in context.Guild.Users.ToArray())
                    {
                        if (!user.Id.ToString().Equals(userId)) continue;
                        var guildAvatar = user.GetGuildAvatarUrl();

                        if (guildAvatar != null) return guildAvatar;
                        var avatar = user.GetAvatarUrl();
                        return avatar ?? user.GetDefaultAvatarUrl();
                    }
                }
                
            }

            // Search for the most recently sent image
            var sortedMessages = context.Channel.GetMessagesAsync().FlattenAsync().Result.OrderBy(m => m.Timestamp);
            foreach (var message in sortedMessages)
            {
                if (!message.Type.Equals(MessageType.Default) && !message.Type.Equals(MessageType.Reply) &&
                    !message.Type.Equals(MessageType.ApplicationCommand)) continue;
                if (message.Attachments != null)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        if (await IsSupportedImage(attachment.Url))
                        {
                            return attachment.Url;
                        }
                            
                    }
                } else if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    foreach (var part in message.Content.Split(' '))
                    {
                        try
                        {
                            var uri = new Uri(part);
                            switch (uri.Host.ToLower())
                            {
                                case GiphyDomain:
                                    return GetGiphyGif(part);
                                case ImgurDomain:
                                    return GetImgurGif(part);
                                case TenorDomain:
                                    return GetTenorGif(part);
                                default:
                                    if (await IsSupportedImage(part))
                                    {
                                        return part;
                                    }
                                    break;
                            }
                        } catch {}
                    }
                } else if (message.Embeds != null)
                {
                    foreach (var embed in message.Embeds)
                    {
                        if (!string.IsNullOrWhiteSpace(embed.Url))
                        {
                            try
                            {
                                var uri = new Uri(embed.Url);
                                switch (uri.Host.ToLower())
                                {
                                    case GiphyDomain:
                                        return GetGiphyGif(embed.Url);
                                    case ImgurDomain:
                                        return GetImgurGif(embed.Url);
                                    case TenorDomain:
                                        return GetTenorGif(embed.Url);
                                    default:
                                        if (await IsSupportedImage(embed.Url))
                                        {
                                            return embed.Url;
                                        }
                                        break;
                                }
                            } catch {}
                        } else if (embed.Image != null && await IsSupportedImage(embed.Image.Value.Url))
                        {
                            return embed.Image.Value.Url;
                        } else if (embed.Thumbnail != null && await IsSupportedImage(embed.Thumbnail.Value.Url))
                        {
                            return embed.Thumbnail.Value.Url;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if file located at an HTTP URL is an image.
        /// </summary>
        /// <param name="url">HTTP URL</param>
        /// <returns>True if an image</returns>
        public async Task<bool> IsSupportedImage(string url)
        {
            try
            {
                var uri = new Uri(url);
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Head
                };
                request.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response.Headers.GetValues("Content-Type").Any(x => x.StartsWith("image/"));
            }
            catch
            {
                return false;
            }
        }
    }
}
