using Discord;
using Discord.Commands;
using ImageMagick;
using MariBot.Common.Model.GpuWorker;
using WaterTrans.GlyphLoader;

namespace MariBot.Worker.CommandHandlers
{
    public class MagickImageHandler
    {
        private static readonly int SIZE_LIMIT_BYTES = 25000000;

        private readonly OpenCVHandler openCvHandler;

        public MagickImageHandler(OpenCVHandler openCvHandler)
        {
            this.openCvHandler = openCvHandler;
        }

        /// <summary>
        /// Converts a input image to a format Discord understands
        /// </summary>
        public void ConvertToDiscordFriendly()
        {
            if (IsAnimated(WorkerGlobals.Job.SourceImage))
            {
                using (var baseImage = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        baseImage.Coalesce();
                        foreach (var frame in baseImage)
                        {
                            outputCollection.Add(new MagickImage(frame));
                        }

                        var outgoingImage = new MemoryStream();
                        outputCollection.Write(outgoingImage, MagickFormat.Gif);

                        WorkerGlobals.Job.Result = new JobResult
                        {
                            FileName = "result.gif",
                            Data = outgoingImage.ToArray()
                        };
                    }
                }
            }
            else
            {
                var inputImage = new MagickImage(WorkerGlobals.Job.SourceImage);
                var outgoingImage = new MemoryStream();
                inputImage.Write(outgoingImage, MagickFormat.Png);

                WorkerGlobals.Job.Result = new JobResult
                {
                    FileName = "result.png",
                    Data = outgoingImage.ToArray()
                };
            }
            
        }

        public void OverlayImage(string filename, double overlayWidthPercentage = .75, double overlayHeightPercentage = .75, bool ignoreAspectRatio = false, Gravity gravity = Gravity.Center)
        {

            bool isAnimated = IsAnimated(WorkerGlobals.Job.SourceImage);

            MemoryStream outgoingImage = new MemoryStream();

            if (isAnimated)
            {
                using (var outputCollection = new MagickImageCollection())
                {
                    using (var baseCollection = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
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
                                frame.Composite(overlay, gravity, CompositeOperator.Over);
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                outputCollection.Add(frame);
                            }
                            outputCollection.Write(outgoingImage, MagickFormat.Gif);
                            outgoingImage.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }

                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
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

                            if (newResized.Length < SIZE_LIMIT_BYTES)
                            {
                                outgoingImage = new MemoryStream();
                                newResized.Seek(0, SeekOrigin.Begin);
                                newResized.CopyTo(outgoingImage);
                                break;
                            }
                        }
                    }
                }
                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        Message = "Failed to process request. Input was too big."
                    };
                }
                else
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        FileName = "result.gif",
                        Data = outgoingImage.ToArray()
                    };
                }
            }
            else
            {
                using (var baseImage = new MagickImage(WorkerGlobals.Job.SourceImage))
                {
                    using (var overlay = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                    {
                        MagickGeometry geometry = new MagickGeometry((int)(baseImage.Width * overlayWidthPercentage),
                            (int)(baseImage.Height * overlayHeightPercentage));
                        geometry.IgnoreAspectRatio = ignoreAspectRatio;
                        overlay.Resize(geometry);
                        baseImage.Composite(overlay, gravity, CompositeOperator.Over);
                        baseImage.Write(outgoingImage, MagickFormat.Png);
                        outgoingImage.Seek(0, SeekOrigin.Begin);
                    }
                }
                WorkerGlobals.Job.Result = new JobResult
                {
                    FileName = "result.png",
                    Data = outgoingImage.ToArray()
                };
            }
        }

        public void AnnotateImage(string filename, MagickReadSettings textSettings, MagickColor transparentColor,
            int x1Dest, int y1Dest, int x2Dest, int y2Dest, int x3Dest, int y3Dest, int x4Dest, int y4Dest)
        {

            MemoryStream outgoingImage = new MemoryStream();
            using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
            {
                using (var overlayImage = new MagickImage($"caption:{WorkerGlobals.Job.SourceText}", textSettings))
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
            WorkerGlobals.Job.Result = new JobResult
            {
                FileName = "result.png",
                Data = outgoingImage.ToArray()
            };
        }

        public void OverlayImage(string filename,
            int x1Dest, int y1Dest, int x2Dest, int y2Dest, int x3Dest, int y3Dest, int x4Dest, int y4Dest)
        {
            int[] xCoords = new int[] { x1Dest, x2Dest, x3Dest, x4Dest };
            int minimumOverlayWidth = xCoords.Max();
            int[] yCoords = new int[] { y1Dest, y2Dest, y3Dest, y4Dest };
            int minimumOverlayHeight = yCoords.Max();

            bool isAnimated = IsAnimated(WorkerGlobals.Job.SourceImage);

            MemoryStream outgoingImage = new MemoryStream();

            if (isAnimated)
            {
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        using (var overlayCollection = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
                        {
                            overlayCollection.Coalesce();
                            foreach (var frame in overlayCollection)
                            {
                                while (frame.Width < minimumOverlayWidth || frame.Height < minimumOverlayHeight)
                                {
                                    frame.Scale(new Percentage(110), new Percentage(110));
                                }
                                int xMax = frame.Width - 1;
                                int yMax = frame.Height - 1;
                                frame.ColorAlpha(new MagickColor(0, 0, 0));
                                frame.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                                frame.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                frame.Crop(baseImage.Width, baseImage.Height);
                                MagickImage newBase = new MagickImage(baseImage);
                                newBase.Composite(frame, CompositeOperator.DstOver);
                                outputCollection.Add(new MagickImage(newBase));
                            }
                        }
                        outputCollection.OptimizeTransparency();
                        outputCollection.Write(outgoingImage, MagickFormat.Gif);
                    }

                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    if (outgoingImage.Length > SIZE_LIMIT_BYTES)
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

                                if (newResized.Length < SIZE_LIMIT_BYTES)
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
                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        Message = "Failed to process request. Input was too big."
                    };
                }
                else
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        FileName = "result.gif",
                        Data = outgoingImage.ToArray()
                    };
                }
            }
            else
            {
                using (var baseImage = new MagickImage(Environment.CurrentDirectory + "\\Content\\" + filename + ".png"))
                {
                    using (var overlayImage = new MagickImage(WorkerGlobals.Job.SourceImage))
                    {
                        while (overlayImage.Width < minimumOverlayWidth || overlayImage.Height < minimumOverlayHeight)
                        {
                            overlayImage.Scale(new Percentage(110), new Percentage(110));
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
                WorkerGlobals.Job.Result = new JobResult
                {
                    FileName = "result.png",
                    Data = outgoingImage.ToArray()
                };
            }
        }

        public void AppendFooter(string filename)
        {
            bool isAnimated = IsAnimated(WorkerGlobals.Job.SourceImage);

            MemoryStream outgoingImage = new MemoryStream();

            if (isAnimated)
            {
                using (var outputCollection = new MagickImageCollection())
                {
                    using (var baseCollection = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
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

                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
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

                            if (newResized.Length < SIZE_LIMIT_BYTES)
                            {
                                outgoingImage = new MemoryStream();
                                newResized.Seek(0, SeekOrigin.Begin);
                                newResized.CopyTo(outgoingImage);
                                break;
                            }
                        }
                    }
                }
                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        Message = "Failed to process request. Input was too big."
                    };
                }
                else
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        FileName = "result.gif",
                        Data = outgoingImage.ToArray()
                    };
                }
            }
            else
            {
                using (var baseImage = new MagickImage(WorkerGlobals.Job.SourceImage))
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
                WorkerGlobals.Job.Result = new JobResult
                {
                    FileName = "result.png",
                    Data = outgoingImage.ToArray()
                };
            }
        }

        public string GetBestFont(string text)
        {
            //List<string> fontList = MariBot.Program.config.GetSection("supportedFonts").GetChildren().Select(t => t.Value).ToList();
            // TODO: Make this configurable
            List<string> fontList = new List<string>()
            {
                "C:\\Windows\\Fonts\\NotoSans-Regular.ttf",
                "C:\\Windows\\Fonts\\NotoSansJP-Regular.otf",
                "C:\\Windows\\Fonts\\NotoSansKR-Regular.otf",
                "C:\\Windows\\Fonts\\NotoSansSC-Regular.otf",
                "C:\\Windows\\Fonts\\NotoSansTC-Regular.otf"
            };
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
                using (var fontStream = System.IO.File.OpenRead(font))
                {
                    var typeface = new Typeface(fontStream);
                    IDictionary<int, ushort> characterMap = typeface.CharacterToGlyphMap;
                    int score = 0;
                    foreach (char c in uniqueCharacters)
                    {
                        if (characterMap != null && characterMap.ContainsKey((int)c))
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

        public void DeepfryImage(int times = 1)
        {
            //Console.WriteLine($"{brightness} {contrast} {saturation} {noise} {jpeg} {sharpen}");
            var random = new Random(GuidToRandomSeed(WorkerGlobals.Job.Id));
            try
            {
                bool isAnimated = IsAnimated(WorkerGlobals.Job.SourceImage);

                MemoryStream outgoingImage = new MemoryStream();

                if (isAnimated)
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        using (var overlayCollection = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
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
                                        newFrame.Depth = 8;
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
                    if (outgoingImage.Length > SIZE_LIMIT_BYTES)
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

                                if (newResized.Length < SIZE_LIMIT_BYTES)
                                {
                                    outgoingImage = new MemoryStream();
                                    newResized.Seek(0, SeekOrigin.Begin);
                                    newResized.CopyTo(outgoingImage);
                                    break;
                                }
                            }
                        }
                    }
                    if (outgoingImage.Length > SIZE_LIMIT_BYTES)
                    {
                        WorkerGlobals.Job.Result = new JobResult
                        {
                            Message = "Failed to process request. Input was too big."
                        };
                    }
                    else
                    {
                        WorkerGlobals.Job.Result = new JobResult
                        {
                            FileName = "result.gif",
                            Data = outgoingImage.ToArray()
                        };
                    }
                }
                else
                {

                    MagickImage newImage = new MagickImage(WorkerGlobals.Job.SourceImage);
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
                            newImage.Depth = 8;
                        }
                        newImage.AddNoise(NoiseType.MultiplicativeGaussian, noise);
                        newImage.BrightnessContrast(new Percentage(brightness), new Percentage(contrast));
                        newImage.Modulate(new Percentage(100.0), new Percentage(saturation));
                        newImage.Quality = jpeg;
                        newImage.Sharpen(12, sharpen);
                    }
                    newImage.Write(outgoingImage, MagickFormat.Jpeg);
                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        FileName = "result.jpeg",
                        Data = outgoingImage.ToArray()
                    };
                }
            }
            catch (Exception ex)
            {
                var exMessage = ex.Message;
                if (ex is AggregateException)
                {
                    exMessage = ex.InnerException.Message;
                }
                WorkerGlobals.Job.Result = new JobResult
                {
                    Message = $"Failed to process request. {exMessage}"
                };
            }

        }

        public MagickImage AutoExplode(IMagickImage image)
        {
            var random = new Random(GuidToRandomSeed(WorkerGlobals.Job.Id));
            double implodeAmount = -2.0;
            double facelessBoxPercentage = 0.5;

            MemoryStream memoryStream = new MemoryStream();
            image.ColorSpace = ColorSpace.RGB;
            image.Depth = 32;
            image.Write(memoryStream, MagickFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var faces = openCvHandler.FindFaces();

            if (faces.Length > 0)
            {
                // Explode the face(s)

                using (MagickImage result = new MagickImage(image.ToByteArray()))
                {
                    foreach (var face in faces)
                    {
                        using (MagickImage singleFace = new MagickImage(image.ToByteArray()))
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

                            result.Composite(singleFace, faceGeometry.X, faceGeometry.Y, CompositeOperator.SrcOver);
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
                using (MagickImage result = new MagickImage(image.ToByteArray()))
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

                    using (MagickImage box = new MagickImage(image.ToByteArray()))
                    {
                        MagickGeometry boxGeometry = new MagickGeometry(x, y, desiredBoxSize, desiredBoxSize);
                        box.Crop(boxGeometry);
                        box.Implode(implodeAmount, PixelInterpolateMethod.Average);
                        var xOffset = x - (result.Width / 2);
                        var yOffset = y - (result.Height / 2);
                        var geometryPositiveX = xOffset >= 0 ? "+" : "";
                        var geometryPositiveY = yOffset >= 0 ? "+" : "";
                        result.Composite(box, CompositeOperator.SrcOver,
                            $"-geometry {geometryPositiveX}{xOffset}{geometryPositiveY}{yOffset}");
                        //result.Composite(box, new PointD(x, y), CompositeOperator.SrcOver);
                    }

                    // image.Dispose();
                    //  bitmap.Dispose();
                    return new MagickImage(result);
                }
            }

        }

        public void ReverseOverlayImage(string filename,
    int x1Dest, int y1Dest, int x2Dest, int y2Dest, int x3Dest, int y3Dest, int x4Dest, int y4Dest)
        {
            int[] xCoords = new int[] { x1Dest, x2Dest, x3Dest, x4Dest };
            int minimumOverlayWidth = xCoords.Max();
            int[] yCoords = new int[] { y1Dest, y2Dest, y3Dest, y4Dest };
            int minimumOverlayHeight = yCoords.Max();

            bool isAnimated = IsAnimated(WorkerGlobals.Job.SourceImage);

            MemoryStream outgoingImage = new MemoryStream();

            if (isAnimated)
            {
                using (var baseImage = new MagickImageCollection(Environment.CurrentDirectory + "\\Content\\" + filename + ".gif"))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        using (var overlayCollection = new MagickImageCollection(WorkerGlobals.Job.SourceImage))
                        {
                            baseImage.Coalesce();
                            overlayCollection.Coalesce();

                            var overlayEnumerator = overlayCollection.GetEnumerator();

                            foreach (var baseFrame in baseImage)
                            {
                                if (!overlayEnumerator.MoveNext())
                                {
                                    overlayEnumerator = overlayCollection.GetEnumerator();
                                    overlayEnumerator.MoveNext();
                                }
                                MagickImage frame = new MagickImage(overlayEnumerator.Current);

                                while (frame.Width < minimumOverlayWidth || frame.Height < minimumOverlayHeight)
                                {
                                    frame.Scale(new Percentage(110), new Percentage(110));
                                }
                                int xMax = frame.Width - 1;
                                int yMax = frame.Height - 1;
                                frame.ColorAlpha(new MagickColor(0, 0, 0));
                                frame.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                                frame.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                                frame.GifDisposeMethod = GifDisposeMethod.None;
                                frame.Crop(baseFrame.Width, baseFrame.Height);
                                MagickImage newBase = new MagickImage(baseFrame);
                                newBase.Composite(frame, CompositeOperator.DstOver);
                                outputCollection.Add(new MagickImage(newBase));
                            }
                        }
                        outputCollection.OptimizeTransparency();
                        outputCollection.Write(outgoingImage, MagickFormat.Gif);
                    }

                    outgoingImage.Seek(0, SeekOrigin.Begin);
                    if (outgoingImage.Length > SIZE_LIMIT_BYTES)
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

                                if (newResized.Length < SIZE_LIMIT_BYTES)
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
                if (outgoingImage.Length > SIZE_LIMIT_BYTES)
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        Message = "Failed to process request. Input was too big."
                    };
                }
                else
                {
                    WorkerGlobals.Job.Result = new JobResult
                    {
                        FileName = "result.gif",
                        Data = outgoingImage.ToArray()
                    };
                }
            }
            else
            {
                using (var baseImage = new MagickImageCollection(Environment.CurrentDirectory + "\\Content\\" + filename + ".gif"))
                {
                    using (var outputCollection = new MagickImageCollection())
                    {
                        using (var overlayImage = new MagickImage(WorkerGlobals.Job.SourceImage))
                        {
                            while (overlayImage.Width < minimumOverlayWidth || overlayImage.Height < minimumOverlayHeight)
                            {
                                overlayImage.Scale(new Percentage(110), new Percentage(110));
                            }
                            int xMax = overlayImage.Width - 1;
                            int yMax = overlayImage.Height - 1;
                            overlayImage.ColorAlpha(new MagickColor(0, 0, 0));
                            overlayImage.VirtualPixelMethod = VirtualPixelMethod.Transparent;
                            overlayImage.Distort(DistortMethod.Perspective, new double[] { 0, 0, x1Dest, y1Dest, xMax, 0, x2Dest, y2Dest, 0, yMax, x3Dest, y3Dest, xMax, yMax, x4Dest, y4Dest });
                            foreach (var frame in baseImage)
                            {
                                MagickImage newBase = new MagickImage(frame);
                                newBase.GifDisposeMethod = GifDisposeMethod.None;
                                newBase.Composite(overlayImage, CompositeOperator.DstOver);
                                outputCollection.Add(newBase);
                            }

                        }
                        outputCollection.OptimizeTransparency();
                        outputCollection.Write(outgoingImage, MagickFormat.Gif);
                    }

                }
                WorkerGlobals.Job.Result = new JobResult
                {
                    FileName = "result.gif",
                    Data = outgoingImage.ToArray()
                };
            }
        }

        /// <summary>
        /// Checks if an input image is animated
        /// </summary>
        /// <param name="incomingImage">Input image</param>
        /// <returns></returns>
        public static bool IsAnimated(byte[] incomingImage)
        {
            bool isAnimated;
            using (var overlayCollection = new MagickImageCollection(incomingImage))
            {
                isAnimated = overlayCollection.Count > 1;
            }

            return isAnimated;
        }

        private static int GuidToRandomSeed(Guid guid)
        {
            int seed = 0;
            foreach (var character in guid.ToByteArray())
            {
                seed += character;
            }
            return seed;
        }
    }
}
