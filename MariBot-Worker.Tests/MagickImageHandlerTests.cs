using System;
using System.IO;
using MariBot.Worker.CommandHandlers;
using MariBot.Common.Model.GpuWorker;
using Xunit;
using Xunit.Abstractions;
using ImageMagick;

namespace MariBot.Worker.Tests
{
    public class MagickImageHandlerTests
    {
        private readonly ITestOutputHelper output;
        private readonly string testOutputsDir;

        public MagickImageHandlerTests(ITestOutputHelper output)
        {
            this.output = output;
            // Use assembly-wide fixture instance
            testOutputsDir = AssemblyTestOutputs.Instance.TestOutputsDir;
            var baseDir = AssemblyTestOutputs.Instance.BaseDir;

            // Ensure supporting directories exist
            Directory.CreateDirectory(Path.Combine(baseDir, "temp"));
            Directory.CreateDirectory(testOutputsDir);
            output.WriteLine($"TestOutputs directory: {testOutputsDir}");
        }

        [Fact]
        public void IsAnimatedReturnsFalseForPng()
        {
            // create a small png in memory
            var ms = new MemoryStream();
            using (var img = new MagickImage(MagickColors.AliceBlue, 100, 50))
            {
                img.Format = MagickFormat.Png;
                img.Write(ms);
            }
            var bytes = ms.ToArray();

            var result = MagickImageHandler.IsAnimated(bytes);
            Assert.False(result);
        }

        [Fact]
        public void ConvertToDiscordFriendlyCreatesPngResult()
        {
            // prepare job
            WorkerGlobals.Job = new WorkerJob
            {
                Id = Guid.NewGuid(),
                SourceImage = CreateTestPng(200, 100)
            };

            var handler = new MagickImageHandler(new OpenCVHandler());
            handler.ConvertToDiscordFriendly();

            Assert.NotNull(WorkerGlobals.Job.Result);
            Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);
            Assert.NotNull(WorkerGlobals.Job.Result.Data);

            var outPath = Path.Combine(testOutputsDir, $"{nameof(ConvertToDiscordFriendlyCreatesPngResult)}.png");
            File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
            
            output.WriteLine($"Wrote generated image to: {outPath}");
        }

        [Fact]
        public void OverlayImage_CoversTopHalf_WhenGravityNorth_WithPercentages()
        {
            // Arrange: create a white base image 100x100
            int width = 100;
            int height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            // Write overlay source in Content directory (small black box)
            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob
                {
                    Id = Guid.NewGuid(),
                    SourceImage = msBase.ToArray()
                };

                var handler = new MagickImageHandler(new OpenCVHandler());

                // Act: overlay should cover the top 50% of the base image when width=100%, height=50% with gravity North
                handler.OverlayImage("blackbox", overlayWidthPercentage: 1.0, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.North);

                // Assert: resulting image exists
                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);
                Assert.NotNull(WorkerGlobals.Job.Result.Data);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                // sample a pixel in the top quarter -> should be black (overlay)
                var topColorObj = pixels.GetPixel(width / 2, height / 4).ToColor();
                dynamic topColorDyn = topColorObj;
                output.WriteLine($"Top sample color: R={topColorDyn.R} G={topColorDyn.G} B={topColorDyn.B}");
                Assert.True(IsBlack(topColorObj), "Expected top area to be black (overlay)");

                // sample a pixel in the bottom quarter -> should be white (no overlay)
                var bottomColorObj = pixels.GetPixel(width / 2, (3 * height) / 4).ToColor();
                dynamic bottomColorDyn = bottomColorObj;
                output.WriteLine($"Bottom sample color: R={bottomColorDyn.R} G={bottomColorDyn.G} B={bottomColorDyn.B}");
                Assert.True(IsWhite(bottomColorObj), "Expected bottom area to be white (background)");

                // also write result file for manual inspection
                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversTopHalf_WhenGravityNorth_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay result to: {outPath}");
            }
            finally
            {
                try { File.Delete(overlayPath); } catch { }
            }
        }

        [Fact]
        public void OverlayImage_CoversLeftHalf_WhenGravityWest_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // left half: width 50%, full height, gravity West
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 1.0, ignoreAspectRatio: true, gravity: Gravity.West);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                var leftColorObj = pixels.GetPixel(width / 4, height / 2).ToColor();
                dynamic leftDyn = leftColorObj;
                output.WriteLine($"Left sample color: R={leftDyn.R} G={leftDyn.G} B={leftDyn.B}");
                Assert.True(IsBlack(leftColorObj), "Expected left area to be black (overlay)");

                var rightColorObj = pixels.GetPixel(3 * width / 4, height / 2).ToColor();
                dynamic rightDyn = rightColorObj;
                output.WriteLine($"Right sample color: R={rightDyn.R} G={rightDyn.G} B={rightDyn.B}");
                Assert.True(IsWhite(rightColorObj), "Expected right area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversLeftHalf_WhenGravityWest_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay west result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversLeftHalf_WhenGravityWest_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversRightHalf_WhenGravityEast_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // right half: width 50%, full height, gravity East
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 1.0, ignoreAspectRatio: true, gravity: Gravity.East);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                var rightColorObj = pixels.GetPixel(3 * width / 4, height / 2).ToColor();
                dynamic rightDyn = rightColorObj;
                output.WriteLine($"Right sample color: R={rightDyn.R} G={rightDyn.G} B={rightDyn.B}");
                Assert.True(IsBlack(rightColorObj), "Expected right area to be black (overlay)");

                var leftColorObj = pixels.GetPixel(width / 4, height / 2).ToColor();
                dynamic leftDyn = leftColorObj;
                output.WriteLine($"Left sample color: R={leftDyn.R} G={leftDyn.G} B={leftDyn.B}");
                Assert.True(IsWhite(leftColorObj), "Expected left area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversRightHalf_WhenGravityEast_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay east result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversRightHalf_WhenGravityEast_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversBottomHalf_WhenGravitySouth_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // bottom half: full width, height 50%, gravity South
                handler.OverlayImage("blackbox", overlayWidthPercentage: 1.0, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.South);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                var bottomColorObj = pixels.GetPixel(width / 2, (3 * height) / 4).ToColor();
                dynamic bottomDyn = bottomColorObj;
                output.WriteLine($"Bottom sample color: R={bottomDyn.R} G={bottomDyn.G} B={bottomDyn.B}");
                Assert.True(IsBlack(bottomColorObj), "Expected bottom area to be black (overlay)");

                var topColorObj = pixels.GetPixel(width / 2, height / 4).ToColor();
                dynamic topDyn = topColorObj;
                output.WriteLine($"Top sample color: R={topDyn.R} G={topDyn.G} B={topDyn.B}");
                Assert.True(IsWhite(topColorObj), "Expected top area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversBottomHalf_WhenGravitySouth_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay south result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversBottomHalf_WhenGravitySouth_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversNorthWestQuarter_WhenGravityNorthWest_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // NW quarter: width 50%, height 50%, gravity NorthWest
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.Northwest);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                // sample inside NW quarter (25,25) -> black
                var nwColorObj = pixels.GetPixel(width / 4, height / 4).ToColor();
                dynamic nwDyn = nwColorObj;
                output.WriteLine($"NW sample color: R={nwDyn.R} G={nwDyn.G} B={nwDyn.B}");
                Assert.True(IsBlack(nwColorObj), "Expected NW area to be black (overlay)");

                // sample inside SE quarter (75,75) -> white
                var seColorObj = pixels.GetPixel(3 * width / 4, 3 * height / 4).ToColor();
                dynamic seDyn = seColorObj;
                output.WriteLine($"SE sample color: R={seDyn.R} G={seDyn.G} B={seDyn.B}");
                Assert.True(IsWhite(seColorObj), "Expected SE area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversNorthWestQuarter_WhenGravityNorthWest_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay nw result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversNorthWestQuarter_WhenGravityNorthWest_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversNorthEastQuarter_WhenGravityNorthEast_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // NE quarter: width 50%, height 50%, gravity NorthEast
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.Northeast);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                // sample inside NE quarter (75,25) -> black
                var neColorObj = pixels.GetPixel(3 * width / 4, height / 4).ToColor();
                dynamic neDyn = neColorObj;
                output.WriteLine($"NE sample color: R={neDyn.R} G={neDyn.G} B={neDyn.B}");
                Assert.True(IsBlack(neColorObj), "Expected NE area to be black (overlay)");

                // sample inside SW quarter (25,75) -> white
                var swColorObj = pixels.GetPixel(width / 4, 3 * height / 4).ToColor();
                dynamic swDyn = swColorObj;
                output.WriteLine($"SW sample color: R={swDyn.R} G={swDyn.G} B={swDyn.B}");
                Assert.True(IsWhite(swColorObj), "Expected SW area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversNorthEastQuarter_WhenGravityNorthEast_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay ne result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversNorthEastQuarter_WhenGravityNorthEast_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversSouthWestQuarter_WhenGravitySouthWest_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // SW quarter: width 50%, height 50%, gravity SouthWest
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.Southwest);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                // sample inside SW quarter (25,75) -> black
                var swColorObj = pixels.GetPixel(width / 4, 3 * height / 4).ToColor();
                dynamic swDyn = swColorObj;
                output.WriteLine($"SW sample color: R={swDyn.R} G={swDyn.G} B={swDyn.B}");
                Assert.True(IsBlack(swColorObj), "Expected SW area to be black (overlay)");

                // sample inside NE quarter (75,25) -> white
                var neColorObj = pixels.GetPixel(3 * width / 4, height / 4).ToColor();
                dynamic neDyn = neColorObj;
                output.WriteLine($"NE sample color: R={neDyn.R} G={neDyn.G} B={neDyn.B}");
                Assert.True(IsWhite(neColorObj), "Expected NE area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversSouthWestQuarter_WhenGravitySouthWest_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay sw result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversSouthWestQuarter_WhenGravitySouthWest_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        [Fact]
        public void OverlayImage_CoversSouthEastQuarter_WhenGravitySouthEast_WithPercentages()
        {
            int width = 100, height = 100;
            using var baseImg = new MagickImage(MagickColors.White, width, height);
            using var msBase = new MemoryStream();
            baseImg.Format = MagickFormat.Png;
            baseImg.Write(msBase);

            var contentDir = Path.Combine(Environment.CurrentDirectory, "Content");
            Directory.CreateDirectory(contentDir);
            var overlayPath = Path.Combine(contentDir, "blackbox.png");
            using (var overlay = new MagickImage(MagickColors.Black, 10, 10))
            {
                overlay.Format = MagickFormat.Png;
                overlay.Write(overlayPath);
            }

            try
            {
                WorkerGlobals.Job = new WorkerJob { Id = Guid.NewGuid(), SourceImage = msBase.ToArray() };
                var handler = new MagickImageHandler(new OpenCVHandler());

                // SE quarter: width 50%, height 50%, gravity SouthEast
                handler.OverlayImage("blackbox", overlayWidthPercentage: 0.5, overlayHeightPercentage: 0.5, ignoreAspectRatio: true, gravity: Gravity.Southeast);

                Assert.NotNull(WorkerGlobals.Job.Result);
                Assert.Equal("result.png", WorkerGlobals.Job.Result.FileName);

                using var resultImg = new MagickImage(WorkerGlobals.Job.Result.Data);
                var pixels = resultImg.GetPixels();

                // sample inside SE quarter (75,75) -> black
                var seColorObj = pixels.GetPixel(3 * width / 4, 3 * height / 4).ToColor();
                output.WriteLine($"SE sample color: R={seColorObj.R} G={seColorObj.G} B={seColorObj.B}");
                Assert.True(IsBlack(seColorObj), "Expected SE area to be black (overlay)");

                // sample inside NW quarter (25,25) -> white
                var nwColorObj = pixels.GetPixel(width / 4, height / 4).ToColor();
                output.WriteLine($"NW sample color: R={nwColorObj.R} G={nwColorObj.G} B={nwColorObj.B}");
                Assert.True(IsWhite(nwColorObj), "Expected NW area to be white (background)");

                var outPath = Path.Combine(testOutputsDir, $"{nameof(OverlayImage_CoversSouthEastQuarter_WhenGravitySouthEast_WithPercentages)}.png");
                File.WriteAllBytes(outPath, WorkerGlobals.Job.Result.Data);
                output.WriteLine($"Wrote overlay se result to: {Path.Combine(testOutputsDir, nameof(OverlayImage_CoversSouthEastQuarter_WhenGravitySouthEast_WithPercentages) + ".png")}");
            }
            finally { try { File.Delete(overlayPath); } catch { } }
        }

        private static byte[] CreateTestPng(int width, int height)
        {
            using (var img = new MagickImage(MagickColors.LightCoral, width, height))
            {
                img.Format = MagickFormat.Png;
                using var ms = new MemoryStream();
                img.Write(ms);
                return ms.ToArray();
            }
        }

        private static bool IsBlack(object c)
        {
            dynamic d = c;
            double r = Convert.ToDouble(d.R);
            double g = Convert.ToDouble(d.G);
            double b = Convert.ToDouble(d.B);
            return r < 1.0 && g < 1.0 && b < 1.0;
        }

        private static bool IsWhite(object c)
        {
            dynamic d = c;
            double r = Convert.ToDouble(d.R);
            double g = Convert.ToDouble(d.G);
            double b = Convert.ToDouble(d.B);
            double max = (double)Quantum.Max;
            return (r >= max * 0.98) && (g >= max * 0.98) && (b >= max * 0.98);
        }
    }
}
