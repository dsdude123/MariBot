using CSharpMath.SkiaSharp;
using Discord;
using Discord.Commands;
using FaceRecognitionDotNet;
using HtmlAgilityPack;
using ImageMagick;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MariBot.Services
{
    public class PictureService
    {
        private static readonly string[] supportedExtensions = new string[] { ".png", ".gif", ".jpeg", ".jpg", ".bmp", ".jfif", ".webp", ".apng" };
        private static readonly string[] animatedExtensions = new string[] { ".gif", ".png", ".apng", ".webp" };
        private static readonly string TenorDomain = "tenor.com";
        private static readonly string ImgurDomain = "imgur.com";
        private readonly HttpClient _http;
        private FaceRecognition finder = FaceRecognition.Create("./FaceModels");
        private Random random = new Random();

        public PictureService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetPictureAsync(string URL)
        {
            var resp = await _http.GetAsync(URL);
            return await resp.Content.ReadAsStreamAsync();
        }

        // TODO: Add support for changing color output (.TextColor)
        public async Task<Stream> GetLatexImage(String latex)
        {
            var painter = new MathPainter();
            painter.LaTeX = latex;
            return painter.DrawAsStream(format: SKEncodedImageFormat.Png);
        }

        public string GetNewestImageUrl(IMessage[] messages)
        {
            foreach (IMessage m in messages)
            {
                if (m.Type.Equals(MessageType.Default))
                {
                    if (m.Attachments != null && m.Attachments.Count > 0)
                    {
                        foreach (IAttachment attachment in m.Attachments)
                        {
                            if (IsSupportedImage(attachment.Url))
                            {
                                return attachment.Url;
                            }
                        }
                    }
                    if (String.IsNullOrWhiteSpace(m.Content))
                    {
                        foreach (string s in m.Content.Split(' '))
                        {
                            if (IsSupportedImage(s))
                            {
                                try
                                {
                                    Uri u = new Uri(s);
                                    if (u.Host.ToLower().Equals(TenorDomain))
                                    {
                                        return GetTenorGif(s);
                                    }
                                    else if (u.Host.ToLower().Equals(ImgurDomain))
                                    {
                                        return GetImgurGif(s);
                                    }
                                    return u.AbsoluteUri;
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
                            if (e.Url != null && IsSupportedImage(e.Url))
                            {
                                try
                                {
                                    Uri u = new Uri(e.Url);
                                    if (u.Host.ToLower().Equals(TenorDomain))
                                    {
                                        return GetTenorGif(e.Url);
                                    }
                                    else if (u.Host.ToLower().Equals(ImgurDomain))
                                    {
                                        return GetImgurGif(e.Url);
                                    }
                                    return u.AbsoluteUri;
                                }
                                catch (Exception ex)
                                {
                                    // Do nothing, not a valid URI
                                }
                            }
                            else if (e.Image != null && IsSupportedImage(e.Image.Value.Url))
                            {
                                return e.Image.Value.Url;
                            }
                            else if (e.Thumbnail != null && IsSupportedImage(e.Thumbnail.Value.Url))
                            {
                                return e.Thumbnail.Value.Url;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public string GetImageUrl(SocketCommandContext context, string url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                var messages = context.Channel.GetMessagesAsync(100, Discord.CacheMode.AllowDownload).FlattenAsync().Result.ToArray();
                messages.OrderBy(message => message.Timestamp);
                return GetNewestImageUrl(messages);
            }
            else
            {
                Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
                if (userIdCheck.IsMatch(url))
                {
                    string userId = url.Replace("<@!", "").Replace(">", "");
                    foreach (var user in context.Guild.Users.ToArray())
                    {
                        if (user.Id.ToString().Equals(userId))
                        {
                            return new Uri(user.GetAvatarUrl()).AbsolutePath;
                        }
                    }
                }


                if (!IsSupportedImage(url))
                {
                    throw new NotSupportedException("Invalid image specified or no image within 100 posts detected.");
                }

                Uri u = new Uri(url);
                if (u.Host.ToLower().Equals(TenorDomain))
                {
                    return GetTenorGif(url);
                }
                else if (u.Host.ToLower().Equals(ImgurDomain))
                {
                    return GetImgurGif(url);
                }
                return url;
            }
        }

        public string GetTenorGif(string url)
        {
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";
            HtmlDocument tenorBase = web.Load(url);
            Uri contentUrl = new Uri(tenorBase.DocumentNode.SelectSingleNode("//meta[@itemprop='contentUrl']").Attributes["content"].Value.ToString());
            if (supportedExtensions.Any(x => contentUrl.AbsolutePath.ToLower().EndsWith(x)))
            {
                return contentUrl.AbsoluteUri;
            }
            else
            {
                throw new NotSupportedException("Tenor image has unsupported extension.");
            }
        }

        public string GetImgurGif(string url)
        {
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";
            HtmlDocument tenorBase = web.Load(url);
            Uri contentUrl = new Uri(tenorBase.DocumentNode.SelectSingleNode("//meta[@property='og:url']").Attributes["content"].Value.ToString());
            if (supportedExtensions.Any(x => contentUrl.AbsolutePath.ToLower().EndsWith(x)))
            {
                return contentUrl.AbsoluteUri;
            }
            else
            {
                throw new NotSupportedException("Imgur image has unsupported extension.");
            }
        }

        public bool IsSupportedImage(string url)
        {

            try
            {
                Uri uri = new Uri(url);
                if (uri.Host.ToLower().Equals(TenorDomain) || uri.Host.ToLower().Equals(ImgurDomain))
                {
                    return true;
                }
                if (supportedExtensions.Any(x => uri.AbsolutePath.ToLower().EndsWith(x)))
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<MemoryStream> GetWebResource(string url)
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

        public async void OverlayImage(SocketCommandContext context, string url, string filename, double overlayWidthPercentage = .75, double overlayHeightPercentage = .75, bool ignoreAspectRatio = false)
        {
            try
            {
                url = GetImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = new MemoryStream();
            try
            {
                incomingImage = GetWebResource(url).Result;
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync(ex.Message);
                return;
            }
            incomingImage.Seek(0, SeekOrigin.Begin);

            bool isAnimated = false;
            Uri uri = new Uri(url);
            if (animatedExtensions.Any(x => uri.AbsolutePath.ToLower().EndsWith(x)))
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
                context.Channel.SendMessageAsync("This might take awhile. I'm working on it.");

                using (var outputCollection = new MagickImageCollection())
                {
                    using (var baseCollection = new MagickImageCollection(incomingImage))
                    {
                        baseCollection.Coalesce();
                        using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                        {
                            MagickGeometry geometry = new MagickGeometry((int)(baseCollection[0].Width * overlayWidthPercentage),
                                (int)(baseCollection[0].Height * overlayHeightPercentage));
                            geometry.IgnoreAspectRatio = ignoreAspectRatio;
                            overlay.Resize(geometry);
                            foreach (var frame in baseCollection)
                            {
                                frame.Composite(overlay, Gravity.Center, CompositeOperator.Over);
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                outputCollection.Add(frame);
                            }
                            outputCollection.Write(outgoingImage, MagickFormat.Gif);
                            outgoingImage.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }

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
                if (outgoingImage.Length > ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                {
                    context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    context.Channel.SendFileAsync(outgoingImage, filename + ".gif");
                }
            }
            else
            {
                using (var baseImage = new MagickImage(incomingImage))
                {
                    using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                    {
                        MagickGeometry geometry = new MagickGeometry((int)(baseImage.Width * overlayWidthPercentage),
                            (int)(baseImage.Height * overlayHeightPercentage));
                        geometry.IgnoreAspectRatio = ignoreAspectRatio;
                        overlay.Resize(geometry);
                        baseImage.Composite(overlay, Gravity.Center, CompositeOperator.Over);
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                        outgoingImage.Seek(0, SeekOrigin.Begin);
                    }
                }
                context.Channel.SendFileAsync(outgoingImage, filename + ".png");
            }
        }

        public async void AnnotateImage(SocketCommandContext context, string filename, string text, MagickReadSettings textSettings, MagickColor transparentColor,
            int x1Dest, int y1Dest, int x2Dest, int y2Dest, int x3Dest, int y3Dest, int x4Dest, int y4Dest)
        {

            MemoryStream outgoingImage = new MemoryStream();
            using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
            {
                using (var overlayImage = new MagickImage($"caption:{text}", textSettings))
                {
                    int xMax = overlayImage.Width - 1;
                    int yMax = overlayImage.Height - 1;
                    overlayImage.ColorAlpha(transparentColor);
                    overlayImage.Transparent(transparentColor);
                    overlayImage.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                    overlayImage.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                    baseImage.Composite(overlayImage, CompositeOperator.SrcOver);
                    baseImage.Format = MagickFormat.Png;
                    baseImage.Write(outgoingImage, MagickFormat.Png);
                }
            }
            outgoingImage.Seek(0, SeekOrigin.Begin);
            context.Channel.SendFileAsync(outgoingImage, filename + ".png");
        }

        public async void OverlayImage(SocketCommandContext context, string url, string filename,
            int x1Dest, int y1Dest, int x2Dest, int y2Dest, int x3Dest, int y3Dest, int x4Dest, int y4Dest)
        {
            int[] xCoords = new int[] { x1Dest, x2Dest, x3Dest, x4Dest };
            int minimumOverlayWidth = xCoords.Max();
            int[] yCoords = new int[] { y1Dest, y2Dest, y3Dest, y4Dest };
            int minimumOverlayHeight = yCoords.Max();
            try
            {
                url = GetImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = new MemoryStream();
            try
            {
                incomingImage = GetWebResource(url).Result;
            } catch (Exception ex)
            {
                await context.Channel.SendMessageAsync(ex.Message);
                return;
            }
            incomingImage.Seek(0, SeekOrigin.Begin);

            bool isAnimated = false;
            Uri uri = new Uri(url);
            if (animatedExtensions.Any(x => uri.AbsolutePath.ToLower().EndsWith(x)))
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
                context.Channel.SendMessageAsync("This might take awhile. I'm working on it.");
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        bool usedBaseOnce = false;
                        using (var overlayCollection = new MagickImageCollection(incomingImage))
                        {
                            overlayCollection.Coalesce();
                            foreach (var frame in overlayCollection)
                            {
                                while (frame.Width < minimumOverlayWidth || frame.Height < minimumOverlayHeight)
                                {
                                    frame.Scale(new Percentage(200), new Percentage(200));
                                }
                                int xMax = frame.Width - 1;
                                int yMax = frame.Height - 1;
                                frame.ColorAlpha(new MagickColor(0, 0, 0));
                                frame.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                                frame.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                frame.Crop(baseImage.Width, baseImage.Height);
                                if (usedBaseOnce)
                                {
                                    outputCollection.Add(new MagickImage(frame));
                                }
                                else
                                {
                                    MagickImage newBase = new MagickImage(baseImage);
                                    newBase.Composite(frame, CompositeOperator.DstOver);
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
                    context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    await context.Channel.SendFileAsync(outgoingImage, filename + ".gif");
                }
            }
            else
            {
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                {
                    using (var overlayImage = new MagickImage(incomingImage))
                    {
                        while (overlayImage.Width < minimumOverlayWidth || overlayImage.Height < minimumOverlayHeight)
                        {
                            overlayImage.Scale(new Percentage(200), new Percentage(200));
                        }
                        int xMax = overlayImage.Width - 1;
                        int yMax = overlayImage.Height - 1;
                        overlayImage.ColorAlpha(new MagickColor(0, 0, 0));
                        overlayImage.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                        overlayImage.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                        baseImage.Composite(overlayImage, CompositeOperator.DstOver);
                        baseImage.Format = MagickFormat.Png;
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                    }
                }
                outgoingImage.Seek(0, SeekOrigin.Begin);
                context.Channel.SendFileAsync(outgoingImage, filename + ".png");
            }
        }

        public async void AppendFooter(SocketCommandContext context, string url, string filename)
        {
            try
            {
                url = GetImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = GetWebResource(url).Result;
            incomingImage.Seek(0, SeekOrigin.Begin);

            bool isAnimated = false;
            Uri uri = new Uri(url);
            if (animatedExtensions.Any(x => uri.AbsolutePath.ToLower().EndsWith(x)))
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
                context.Channel.SendMessageAsync("This might take awhile. I'm working on it.");

                using (var outputCollection = new MagickImageCollection())
                {
                    using (var baseCollection = new MagickImageCollection(incomingImage))
                    {
                        baseCollection.Coalesce();
                        using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                        {
                            MagickGeometry geometry = new MagickGeometry(overlay.Width, overlay.Height);
                            geometry.IgnoreAspectRatio = true;
                            foreach (var frame in baseCollection)
                            {
                                frame.Resize(geometry);
                                frame.Extent(frame.Width, frame.Height + overlay.Height, Gravity.North);
                                frame.Composite(overlay, Gravity.South, CompositeOperator.Over);
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                outputCollection.Add(frame);
                            }
                            outputCollection.Write(outgoingImage, MagickFormat.Gif);
                            outgoingImage.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }

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
                if (outgoingImage.Length > ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                {
                    context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    context.Channel.SendFileAsync(outgoingImage, filename + ".gif");
                }
            }
            else
            {
                using (var baseImage = new MagickImage(incomingImage))
                {
                    using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                    {
                        MagickGeometry geometry = new MagickGeometry(overlay.Width, overlay.Height);
                        geometry.IgnoreAspectRatio = true;
                        baseImage.Resize(geometry);
                        baseImage.Extent(baseImage.Width, baseImage.Height + overlay.Height, Gravity.North);
                        baseImage.Composite(overlay, Gravity.South, CompositeOperator.Over);
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                        outgoingImage.Seek(0, SeekOrigin.Begin);
                    }
                }
                context.Channel.SendFileAsync(outgoingImage, filename + ".png");
            }
        }

        public string GetBestFont(string text)
        {
            List<string> fontList = MariBot.Program.config.GetSection("supportedFonts").GetChildren().Select(t => t.Value).ToList();
            string topFont = "";
            int topScore = 0;

            List<char> uniqueCharacters = new List<char>();

            foreach (char c in text)
            {
                if (!uniqueCharacters.Contains(c))
                {
                    uniqueCharacters.Add(c);
                }
            }

            foreach (string font in fontList)
            {
                FontFamily fontToCheck = new FontFamily(font);
                var typefaces = fontToCheck.GetTypefaces();
                foreach (Typeface typeface in typefaces)
                {
                    GlyphTypeface glyph;
                    typeface.TryGetGlyphTypeface(out glyph);
                    IDictionary<int, ushort> characterMap = glyph.CharacterToGlyphMap;
                    int score = 0;
                    foreach (char c in uniqueCharacters)
                    {
                        ushort val;
                        if (glyph != null && glyph.CharacterToGlyphMap.TryGetValue(c, out val))
                        {
                            score++;
                        }
                    }

                    if (score > topScore)
                    {
                        topFont = font;
                        topScore = score;
                    }

                    if (topScore == uniqueCharacters.Count)
                    {
                        return topFont; // Found a font with everything we need
                    }

                }
            }

            return topFont;
        }

        public async void DeepfryImage(SocketCommandContext context, string url, int times = 1)
        {
            //Console.WriteLine($"{brightness} {contrast} {saturation} {noise} {jpeg} {sharpen}");

            try
            {
                url = GetImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = GetWebResource(url).Result;
            incomingImage.Seek(0, SeekOrigin.Begin);

            bool isAnimated = false;
            Uri uri = new Uri(url);
            if (animatedExtensions.Any(x => uri.AbsolutePath.ToLower().EndsWith(x)))
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
                string longTimeText = times > 1 ? "Oh... I see you gave me a GIF. This is going to take a real loooonnngggg time." : "This might take awhile. I'm working on it.";
                context.Channel.SendMessageAsync(longTimeText);
                using (var outputCollection = new MagickImageCollection())
                {
                    using (var overlayCollection = new MagickImageCollection(incomingImage))
                    {
                        overlayCollection.Coalesce();
                        foreach (var frame in overlayCollection)
                        {
                            MagickImage newFrame = new MagickImage(frame);
                            for (int i = 0; i < times; i++)
                            {
                                int brightness = random.Next(-10, 30);
                                int contrast = (int)((-2.5 * brightness) + 75);
                                int saturation = random.Next(400, 600);
                                double noise = (double)random.Next(1, 8) / 10;
                                int jpeg = random.Next(3, 7);
                                double sharpen = 24;
                                bool reduceBitDepth = random.Next(11) < 6;
                                newFrame = AutoExplode(newFrame);
                                newFrame.Settings.AntiAlias = false;
                                if (reduceBitDepth)
                                {
                                    newFrame.BitDepth(Channels.Red, 3);
                                    newFrame.BitDepth(Channels.Green, 3);
                                    newFrame.BitDepth(Channels.Blue, 2);
                                }
                                newFrame.AddNoise(NoiseType.MultiplicativeGaussian, noise);
                                newFrame.BrightnessContrast(new Percentage(brightness), new Percentage(contrast));
                                newFrame.Modulate(new Percentage(100.0), new Percentage(saturation));
                                newFrame.Quality = jpeg;
                                newFrame.Sharpen(12, sharpen);
                            }
                            outputCollection.Add(new MagickImage(newFrame));
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

                if (outgoingImage.Length > ByteSizeLib.ByteSize.FromMegaBytes(8).Bytes)
                {
                    context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    context.Channel.SendFileAsync(outgoingImage, "deepfry.gif");
                }
            }
            else
            {

                MagickImage newImage = new MagickImage(incomingImage);
                for (int i = 0; i < times; i++)
                {
                    int brightness = random.Next(-10, 30);
                    int contrast = (int)((-2.5 * brightness) + 75);
                    int saturation = random.Next(400, 600);
                    double noise = (double)random.Next(1, 8) / 10;
                    int jpeg = random.Next(3, 7);
                    double sharpen = 24;
                    bool reduceBitDepth = random.Next(11) < 6;
                    newImage = AutoExplode(newImage);
                    newImage.Settings.AntiAlias = false;
                    if (reduceBitDepth)
                    {
                        newImage.BitDepth(Channels.Red, 3);
                        newImage.BitDepth(Channels.Green, 3);
                        newImage.BitDepth(Channels.Blue, 2);
                    }
                    newImage.AddNoise(NoiseType.MultiplicativeGaussian, noise);
                    newImage.BrightnessContrast(new Percentage(brightness), new Percentage(contrast));
                    newImage.Modulate(new Percentage(100.0), new Percentage(saturation));
                    newImage.Quality = jpeg;
                    newImage.Sharpen(12, sharpen);
                }
                newImage.Write(outgoingImage, MagickFormat.Jpeg);
                outgoingImage.Seek(0, SeekOrigin.Begin);
                context.Channel.SendFileAsync(outgoingImage, "deepfry.jpg");
            }
        }

        public MagickImage AutoExplode(IMagickImage image)
        {

            double implodeAmount = -2.0;
            double facelessBoxPercentage = 0.5;

            MemoryStream memoryStream = new MemoryStream();
            image.ColorSpace = ColorSpace.RGB;
            image.Depth = 32;
            image.Write(memoryStream, MagickFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);
            FaceRecognitionDotNet.Image imageToDetect;
            List<Location> faces;

            try
            {
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(memoryStream);
                imageToDetect = FaceRecognition.LoadImage(bitmap);

                faces = finder.FaceLocations(imageToDetect).ToList();
            } catch (Exception ex)
            {
                Console.WriteLine("Failed to run face recognition. " + ex.Message);
                faces = new List<Location>();
            }
            //memoryStream.Dispose();

            if (faces.Count > 0)
            {
                // Explode the face(s)

                using (MagickImage result = new MagickImage(image))
                {
                    foreach (Location face in faces)
                    {
                        using (MagickImage singleFace = new MagickImage(image))
                        {
                            int rectWidth = face.Right - face.Left;
                            int rectHeight = face.Bottom - face.Top;
                            int x = face.Left;
                            int y = face.Top;

                            switch (random.Next(0, 5))
                            {
                                case 0: //Center
                                    if ((x - rectWidth) >= 0 && (y - rectHeight) >= 0 && (x + (rectWidth * 2) < image.Width) && (y + (rectHeight * 2) < image.Height))
                                    {
                                        x -= rectWidth;
                                        y -= rectHeight;
                                        rectWidth *= 2;
                                        rectHeight *= 2;
                                    }
                                    break;
                                case 1: //Top left
                                    if (((x - (rectWidth * 2)) >= 0) && (y - (rectHeight * 2) >= 0))
                                    {
                                        x -= rectWidth * 2;
                                        y -= rectHeight * 2;
                                        rectWidth *= 2;
                                        rectHeight *= 2;
                                    }
                                    else
                                    {
                                        rectWidth /= 2;
                                        rectHeight /= 2;
                                    }
                                    break;
                                case 2: //Top Right
                                    if ((x + (rectWidth * 2.5) < image.Width) && ((y - (rectHeight * 2)) >= 0) && (y + (rectHeight * 1.5) < image.Height))
                                    {
                                        x += rectWidth / 2;
                                        y -= rectHeight * 2;
                                        rectWidth *= 2;
                                        rectHeight *= 2;
                                    }
                                    else
                                    {
                                        x += rectWidth / 2;
                                        rectWidth /= 2;
                                        rectHeight /= 2;
                                    }
                                    break;
                                case 3: // Bottom Left
                                    if (((x - (rectWidth * 2)) >= 0) && ((y + (rectHeight * 2.5)) < image.Height))
                                    {
                                        x -= rectWidth * 2;
                                        y += rectHeight / 2;
                                        rectWidth *= 2;
                                        rectHeight *= 2;
                                    }
                                    else
                                    {
                                        y += rectHeight / 2;
                                        rectWidth /= 2;
                                        rectHeight /= 2;
                                    }
                                    break;
                                case 4: // Bottom Right
                                    if ((x + (rectWidth * 2.5) < image.Width) && (y + (rectHeight * 2.5) < image.Height))
                                    {
                                        x += rectWidth / 2;
                                        y += rectHeight / 2;
                                        rectWidth *= 2;
                                        rectHeight *= 2;
                                    }
                                    else
                                    {
                                        x += rectWidth / 2;
                                        y += rectHeight / 2;
                                        rectWidth /= 2;
                                        rectHeight /= 2;
                                    }
                                    break;
                            }
                            MagickGeometry faceGeometry = new MagickGeometry(x, y, rectWidth, rectHeight);
                            singleFace.Crop(faceGeometry);
                            singleFace.Implode(implodeAmount, PixelInterpolateMethod.Average);
                            result.Composite(singleFace, new PointD(x, y), CompositeOperator.SrcOver);
                        }
                    }
                    // image.Dispose();
                    //bitmap.Dispose();
                    return new MagickImage(result);
                }
            }
            else
            {
                // random location
                using (MagickImage result = new MagickImage(image))
                {
                    int desiredBoxSize = (int)(Math.Min(image.Width, image.Height) * facelessBoxPercentage);

                    int x;
                    int y;

                    do
                    {
                        x = random.Next(0, result.Width);
                    } while (x > (result.Width - desiredBoxSize));

                    do
                    {
                        y = random.Next(0, result.Height);
                    } while (y > (result.Height - desiredBoxSize));

                    using (MagickImage box = new MagickImage(image))
                    {
                        MagickGeometry boxGeometry = new MagickGeometry(x, y, desiredBoxSize, desiredBoxSize);
                        box.Crop(boxGeometry);
                        box.Implode(implodeAmount, PixelInterpolateMethod.Average);
                        result.Composite(box, new PointD(x, y), CompositeOperator.SrcOver);
                    }

                    // image.Dispose();
                    //  bitmap.Dispose();
                    return new MagickImage(result);
                }
            }

        }

        public async void ConvertJfifToJpeg(SocketCommandContext context, string url)
        {
            MemoryStream incomingImage = GetWebResource(url).Result;
            incomingImage.Seek(0, SeekOrigin.Begin);

            MemoryStream outgoingImage = new MemoryStream();

            using (var baseImage = new MagickImage(incomingImage))
            {
                baseImage.Write(outgoingImage, MagickFormat.Jpeg);
            }
            outgoingImage.Seek(0, SeekOrigin.Begin);
            await context.Channel.SendFileAsync(outgoingImage, "autoconvert.jpeg");
        }
    }
}
