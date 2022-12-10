using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using ImageMagick;
using MariBot.Services;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    public class ImageModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }
        public Edges2HentaiService Edges2HentaiService { get; set; }

        public ImageHubService ImageHubService { get; set; }

        [Command("sonicsays", RunMode = RunMode.Async)]
        public async Task sonicsays([Remainder] string text)
        {
            var readSettings = new MagickReadSettings
            {
                TextEncoding = Encoding.Unicode,
                FontFamily = PictureService.GetBestFont(text),
                FontStyle = FontStyleType.Bold,
                FillColor = MagickColors.White,
                BackgroundColor = MagickColors.Black,
                Width = 980,
                Height = 624
            };

            PictureService.AnnotateImage(Context, "sonicsaystemplate", text, readSettings, MagickColors.Black, 41, 93, 539, 93, 41, 406, 539, 406);
            //System.Drawing.Image backing = System.Drawing.Image.FromFile(Environment.CurrentDirectory + "\\Content\\sonicsaystemplate.png");
            //Graphics canvas = Graphics.FromImage(backing);
            //Rectangle r = new Rectangle(new Point(44, 112), new Size(514, 291));
            //StringFormat s = new StringFormat();
            //s.Alignment = StringAlignment.Near;
            //s.LineAlignment = StringAlignment.Center;
            //canvas.DrawString(text, new Font(FontFamily.GenericSansSerif, 50), Brushes.White, r, s);
            //MemoryStream outgoing = new MemoryStream();
            //canvas.Save();
            //backing.Save(outgoing, System.Drawing.Imaging.ImageFormat.Png);
            //outgoing.Seek(0, SeekOrigin.Begin);
            //return Context.Channel.SendFileAsync(outgoing, "sonicsays.png");
        }

        [Command("9gag", RunMode = RunMode.Async)]
        public async Task NineGag(string url = null)
        {
            try
            {
                url = PictureService.GetImageUrl(Context, url);
            }
            catch (NotSupportedException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = PictureService.GetWebResource(url).Result;
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

                using (var outputCollection = new MagickImageCollection())
                {
                    using (var baseCollection = new MagickImageCollection(incomingImage))
                    {
                        baseCollection.Coalesce();
                        using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\9gag.png"))
                        {
                            var newX = baseCollection[0].Width * 0.03;
                            var newY = baseCollection[0].Height * 0.15;

                            overlay.Resize((int)newX, (int)newY);

                            var offsetSpacing = baseCollection[0].Width * 0.04;
                            var xPlacement = baseCollection[0].Width - offsetSpacing;
                            var yPlacement = (baseCollection[0].Height / 2) - (newY / 2);
                            foreach (var frame in baseCollection)
                            {
                                frame.Composite(overlay, (int)xPlacement, (int)yPlacement);
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
                    Context.Channel.SendMessageAsync("Sorry that image was just too big for me to handle.");
                }
                else
                {
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    Context.Channel.SendFileAsync(outgoingImage, "9gag.gif");
                }
            }
            else
            {
                using (var baseImage = new MagickImage(incomingImage))
                {
                    using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\9gag.png"))
                    {
                        var newX = baseImage.Width * 0.03;
                        var newY = baseImage.Height * 0.15;

                        overlay.Resize((int)newX, (int)newY);

                        var offsetSpacing = baseImage.Width * 0.04;
                        var xPlacement = baseImage.Width - offsetSpacing;
                        var yPlacement = (baseImage.Height / 2) - (newY / 2);
                        baseImage.Composite(overlay, (int)xPlacement, (int)yPlacement);
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                        outgoingImage.Seek(0, SeekOrigin.Begin);
                    }
                }
                Context.Channel.SendFileAsync(outgoingImage, "9gag.png");
            }
        }

        [Command("adidas", RunMode = RunMode.Async)]
        public async Task Adidas(string url = null)
        {
            PictureService.OverlayImage(Context, url, "adidas");
        }

        [Command("adw", RunMode = RunMode.Async)]
        public async Task AdminWalk(string url = null)
        {
            PictureService.OverlayImage(Context, url, "adw", 379, 113, 513, 113, 379, 245, 513, 245);
        }

        [Command("ai-image", RunMode = RunMode.Async)]
        public async Task aiimage([Remainder]string prompt)
        {
            ImageHubService.ExecuteTextCommand(Context, "stablediffusion-text", prompt);
        }

        [Command("ai-pokemon", RunMode = RunMode.Async)]
        public async Task aipokemon([Remainder] string prompt)
        {
            ImageHubService.ExecuteTextCommand(Context, "pokemon", prompt);
        }

        [Command("ai-waifu", RunMode = RunMode.Async)]
        public async Task aiwaifu([Remainder] string prompt)
        {
            ImageHubService.ExecuteTextCommand(Context, "waifudiffusion", prompt);
        }

        [Command("ajit", RunMode = RunMode.Async)]
        public async Task Ajit(string url = null)
        {
            PictureService.OverlayImage(Context, url, "ajit", 1, 1);
        }

        [Command("america", RunMode = RunMode.Async)]
        public async Task America(string url = null)
        {
            PictureService.OverlayImage(Context, url, "america", 1, 1, true);
        }

        [Command("analysis", RunMode = RunMode.Async)]
        public async Task Analysis([Remainder] string url = null)
        {
            PictureService.AppendFooter(Context, url, "analysis");
        }

        [Command("austin", RunMode = RunMode.Async)]
        public async Task Austin(string url = null)
        {
            PictureService.OverlayImage(Context, url, "austin", 525, 366, 706, 360, 529, 475, 712, 466);
        }

        [Command("bernie", RunMode = RunMode.Async)]
        public async Task Bernie(string url = null)
        {
            PictureService.OverlayImage(Context, url, "bernie", 294, 93, 638, 67, 306, 374, 649, 349);
        }

        [Command("biden", RunMode = RunMode.Async)]
        public async Task Biden(string url = null)
        {
            var source = new Random().Next(0, 2);

            switch (source)
            {
                case 0:
                    PictureService.OverlayImage(Context, url, "biden", 50, 72, 341, 117, 42, 627, 339, 587);
                    break;
                case 1:
                    PictureService.OverlayImage(Context, url, "biden2", 66, 255, 442, 284, 75, 830, 446, 641);
                    break;
                default:
                    throw new ArithmeticException("Random not properly calculated.");
            }
        }

        [Command("binoculars", RunMode = RunMode.Async)]
        public async Task Binoculars(string url = null)
        {
            PictureService.OverlayImage(Context, url, "binoculars", 36, 458, 769, 458, 36, 894, 769, 894);
        }

        [Command("bobross", RunMode = RunMode.Async)]
        public async Task BobRoss(string url = null)
        {
            PictureService.OverlayImage(Context, url, "bobross", 22, 71, 453, 88, 22, 389, 453, 403);
        }

        [Command("cmm", RunMode = RunMode.Async)]
        public async Task ChangeMyMind([Remainder] string text)
        {
            var readSettings = new MagickReadSettings
            {
                TextEncoding = Encoding.Unicode,
                FontFamily = PictureService.GetBestFont(text),
                FontStyle = FontStyleType.Bold,
                FillColor = MagickColors.Black,
                BackgroundColor = MagickColors.White,
                Width = 980,
                Height = 624
            };

            PictureService.AnnotateImage(Context, "cmm", text, readSettings, MagickColors.White, 242, 352, 526, 236, 292, 474, 576, 358);
        }

        [Command("condom", RunMode = RunMode.Async)]
        public async Task Condom([Remainder] string url = null)
        {
            PictureService.OverlayImage(Context, url, "condom", 0, 381, 320, 381, 0, 702, 320, 702);
        }

        [Command("daryl", RunMode = RunMode.Async)]
        public async Task Daryl(string url = null)
        {
            var source = new Random().Next(0, 9);
            switch (source)
            {
                case 0:
                    PictureService.OverlayImage(Context, url, "daryl", 1043, 565, 1515, 492, 1040, 792, 1517, 801);
                    break;
                case 1:
                    PictureService.OverlayImage(Context, url, "daryl2", 1145, 233, 1941, 151, 1128, 622, 1881, 678);
                    break;
                case 2:
                    PictureService.OverlayImage(Context, url, "daryl3", 223, 483, 525, 502, 181, 1121, 483, 1140);
                    break;
                case 3:
                    PictureService.OverlayImage(Context, url, "daryl4", 275, 376, 343, 414, 203, 499, 271, 538);
                    break;
                case 4:
                    PictureService.OverlayImage(Context, url, "daryl5", 94, 15, 770, 0, 248, 530, 842, 437);
                    break;
                case 5:
                    PictureService.OverlayImage(Context, url, "daryl6", 822, 1347, 882, 1338, 829, 1389, 890, 1379);
                    break;
                case 6:
                    PictureService.OverlayImage(Context, url, "daryl7", 1648, 1528, 2426, 1495, 1655, 2288, 2542, 2264);
                    break;
                case 7:
                    PictureService.OverlayImage(Context, url, "daryl8", 1126, 501, 1940, 311, 1135, 801, 1914, 862);
                    break;
                case 8:
                    PictureService.OverlayImage(Context, url, "daryl9", 1098, 448, 1903, 268, 1100, 754, 1846, 809);
                    break;
                default:
                    throw new ArithmeticException("Random not properly calculated.");
            }
            
        }

        [Command("dave", RunMode = RunMode.Async)]
        public async Task dave(string url = null)
        {
            PictureService.OverlayImage(Context, url, "dave", 895, 258, 1234, 234, 894, 441, 1234, 447);
        }

        [Command("deepfry", RunMode = RunMode.Async)]
        public async Task Deepfry([Remainder] string url = null)
        {
            PictureService.DeepfryImage(Context, url);
        }

        [Command("nuke", RunMode = RunMode.Async)]
        public async Task Nuke([Remainder] string url = null)
        {
            // Glory to the USSR
            Context.Channel.SendMessageAsync("This is going to take quite some time. Hang tight and I'll ping you when I'm done.");
            PictureService.DeepfryImage(Context, url, 10);
            Context.Channel.SendMessageAsync(Context.Message.Author.Mention);
        }

        [Command("e2h", RunMode = RunMode.Async)]
        public async Task Edges2Hentai(string url = null)
        {
            Edges2HentaiService.Edges2Hentai(Context, url);
        }

        [Command("kevin", RunMode = RunMode.Async)]
        public async Task kevin(string url = null)
        {
            PictureService.OverlayImage(Context, url, "kevin", 1119, 363, 1960, 163, 1123, 674, 1831, 749);
        }


        [Command("kurisu", RunMode = RunMode.Async)]
        public async Task Kurisu([Remainder] string text)
        {
            var readSettings = new MagickReadSettings
            {
                TextEncoding = Encoding.Unicode,
                FontFamily = PictureService.GetBestFont(text),
                FontStyle = FontStyleType.Bold,
                FillColor = MagickColors.Black,
                BackgroundColor = MagickColors.White,
                Width = 980,
                Height = 980
            };

            PictureService.AnnotateImage(Context, "kurisu", text, readSettings, MagickColors.White, 32, 74, 241, 74, 32, 280, 241, 280);
        }


        [Command("pence", RunMode = RunMode.Async)]
        public async Task Pence(string url = null)
        {
            PictureService.OverlayImage(Context, url, "pence", 615, 254, 663, 261, 566, 379, 618, 389);
        }

        [Command("queen", RunMode = RunMode.Async)]
        public async Task Queen([Remainder] string text)
        {
            var readSettings = new MagickReadSettings
            {
                TextEncoding = Encoding.Unicode,
                FontFamily = PictureService.GetBestFont(text),
                FontStyle = FontStyleType.Bold,
                FillColor = MagickColors.Black,
                BackgroundColor = MagickColors.White,
                Width = 980,
                Height = 624
            };

            PictureService.AnnotateImage(Context, "queen", text, readSettings, MagickColors.White, 86, 175, 408, 177, 86, 342, 404, 353);
        }

        [Command("scarecrow", RunMode = RunMode.Async)]
        public async Task Scarecrow(string url = null)
        {
            var source = new Random().Next(0, 3);
            switch (source)
            {
                case 0:
                    PictureService.OverlayImage(Context, url, "scarecrow", 1168, 278, 1897, 37, 1159, 567, 1773, 585);
                    break;
                case 1:
                    PictureService.OverlayImage(Context, url, "scarecrow2", 1021, 408, 1957, 291, 1003, 757, 1806, 909);
                    break;
                default:
                    throw new ArithmeticException("Random not properly calculated.");
            }

        }
    }
}
