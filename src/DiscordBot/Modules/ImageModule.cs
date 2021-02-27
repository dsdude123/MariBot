using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using ImageMagick;

namespace MariBot.Modules
{
    public class ImageModule : ModuleBase<SocketCommandContext>
    {
        [Command("sonicsays")]
        public Task sonicsays([Remainder] string text)
        {
            System.Drawing.Image backing = System.Drawing.Image.FromFile(Environment.CurrentDirectory + "\\Content\\sonicsaystemplate.png");
            Graphics canvas = Graphics.FromImage(backing);
            Rectangle r = new Rectangle(new Point(44,112),new Size(514,291) );
            StringFormat s = new StringFormat();
            s.Alignment = StringAlignment.Near;
            s.LineAlignment = StringAlignment.Center;
            canvas.DrawString(text,new Font(FontFamily.GenericSansSerif, 50),Brushes.White,r,s );
            MemoryStream outgoing = new MemoryStream();
            canvas.Save();
            backing.Save(outgoing, System.Drawing.Imaging.ImageFormat.Png);
            outgoing.Seek(0, SeekOrigin.Begin);
            return Context.Channel.SendFileAsync(outgoing, "sonicsays.png");
        }

        [Command("biden")]
        public async Task biden(string url = null)
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

            MemoryStream incomingImage = getWebResource(url).Result;
            incomingImage.Seek(0, SeekOrigin.Begin);

            bool isAnimated = false;
            Uri uri = new Uri(url);
            if (uri.AbsolutePath.EndsWith(".gif"))
            {
                using (var overlayCollection = new MagickImageCollection(incomingImage))
                {
                    isAnimated = overlayCollection.Count > 1;
                    incomingImage.Seek(0, SeekOrigin.Begin);
                }
            }

            MemoryStream outgoingImage = new MemoryStream();

            if (isAnimated)
            {
                Context.Channel.SendMessageAsync("This might take awhile. I'm working on it.");
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\biden.jpg"))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        bool usedBaseOnce = false;
                        using (var overlayCollection = new MagickImageCollection(incomingImage))
                        {
                            overlayCollection.Coalesce();
                            foreach (var frame in overlayCollection)
                            {
                                while (frame.Width < 341 || frame.Height < 627)
                                {
                                    frame.Scale(new Percentage(200), new Percentage(200));
                                }
                                int xMax = frame.Width - 1;
                                int yMax = frame.Height - 1;
                                frame.ColorAlpha(new MagickColorFactory().Create(0, 0, 0));
                                frame.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                                frame.Distort(DistortMethod.Perspective, new double[] { 0, 0, 50, 72, xMax, 0, 341, 117, 0, yMax, 42, 627, xMax, yMax, 339, 587 });
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                frame.Crop(baseImage.Width, baseImage.Height);
                                if (usedBaseOnce)
                                {
                                    outputCollection.Add(new MagickImage(frame));
                                }
                                else
                                {
                                    MagickImage newBase = new MagickImage(baseImage);
                                    newBase.Composite(frame, CompositeOperator.SrcOver);
                                    outputCollection.Add(new MagickImage(newBase));
                                    usedBaseOnce = true;
                                }
                            }
                        }
                        outputCollection.Write(outgoingImage, MagickFormat.Gif);
                    }
                    
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    if (outgoingImage.Length > ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                    {
                        double[] scales = new double[] { 75, 50, 25 };
                        for (int i = 0; i < 3; i++)
                        {
                            using (var outputDownscale = new MagickImageCollection())
                            {
                                using (var inputImage = new MagickImageCollection(outgoingImage))
                                {
                                    foreach (var frame in inputImage)
                                    {
                                        frame.Resize(new Percentage(scales[i]));
                                        outputDownscale.Add(new MagickImage(frame));
                                    }
                                }
                                MemoryStream newResized = new MemoryStream();
                                outputDownscale.Write(newResized, MagickFormat.Gif);

                                if (newResized.Length < ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                                {
                                    outgoingImage = new MemoryStream();
                                    newResized.Seek(0, SeekOrigin.Begin);
                                    newResized.CopyTo(outgoingImage);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (outgoingImage.Length > ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                {
                    Context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    Context.Channel.SendFileAsync(outgoingImage, "biden.gif");
                }
            }
            else
            {
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\biden.jpg"))
                {
                    using (var overlayImage = new MagickImage(incomingImage))
                    {
                        while (overlayImage.Width < 341 || overlayImage.Height < 627)
                        {
                            overlayImage.Scale(new Percentage(200), new Percentage(200));
                        }
                        int xMax = overlayImage.Width - 1;
                        int yMax = overlayImage.Height - 1;
                        overlayImage.ColorAlpha(new MagickColorFactory().Create(0, 0, 0));
                        overlayImage.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                        overlayImage.Distort(DistortMethod.Perspective, new double[] { 0, 0, 50, 72, xMax, 0, 341, 117, 0, yMax, 42, 627, xMax, yMax, 339, 587 });
                        baseImage.Composite(overlayImage, CompositeOperator.SrcOver);
                        baseImage.Format = MagickFormat.Png;
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                    }
                }
                outgoingImage.Seek(0, SeekOrigin.Begin);
                Context.Channel.SendFileAsync(outgoingImage, "biden.png");
            }
        }

        //TODO: Move these methods to a util class
        private string getNewestImageUrl(IMessage[] messages)
        {
            foreach (IMessage m in messages)
            {
                if (m.Type.Equals(MessageType.Default))
                {
                    if (m.Attachments != null && m.Attachments.Count > 0)
                    {
                        if (isSupportedImage(m.Attachments.First().Filename))
                        {
                            return m.Attachments.First().Url;
                        }
                    }
                    if (m.Content != null)
                    {
                        foreach (string s in m.Content.Split(' '))
                        {
                            if (isSupportedImage(s))
                            {
                                try
                                {
                                    Uri u = new Uri(s);
                                    if (u.Host.ToLower().Equals("tenor.com"))
                                    {
                                        return getTenorGif(s);
                                    } else if (u.Host.ToLower().Equals("imgur.com"))
                                    {
                                        return getImgurGif(s);
                                    }
                                    return s;
                                }
                                catch (Exception e)
                                {
                                    // Do nothing, not a valid URI
                                }
                            }
                        }
                    }
                    if (m.Embeds != null)
                    {
                        foreach (Embed e in m.Embeds)
                        {
                            if (e.Image != null)
                            {
                                return e.Image.Value.Url;
                            }
                            if (e.Thumbnail != null)
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

        private string getTenorGif(string url)
        {
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";
            HtmlDocument tenorBase = web.Load(url);
            return tenorBase.DocumentNode.SelectSingleNode("//meta[@itemprop='contentUrl']").Attributes["content"].Value.ToString();       
        }

        private string getImgurGif(string url)
        {
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";
            HtmlDocument tenorBase = web.Load(url);
            return tenorBase.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value.ToString();
        }

        private bool isSupportedImage(string url)
        {
            if (url.ToLower().EndsWith(".png") || url.ToLower().EndsWith(".gif") || url.ToLower().EndsWith(".jpeg") || url.ToLower().EndsWith(".jpg") || url.ToLower().EndsWith(".bmp") || url.ToLower().EndsWith(".jfif"))
            {
                return true;
            }
            try
            {
                Uri uri = new Uri(url);
                if (uri.Host.Equals("tenor.com") || uri.Host.Contains(".tenor.com") || uri.Host.Equals("imgur.com") || uri.Host.Contains(".imgur.com"))
                {
                    return true;
                }
                return false;
            } catch (Exception e)
            {
                return false;
            }
        }

        private async Task<MemoryStream> getWebResource(string url)
        {
            var request = HttpWebRequest.CreateHttp(url);
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
