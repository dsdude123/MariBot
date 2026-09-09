using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MariBot.Worker;
using MariBot.Worker.CommandHandlers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MariBot.Worker.Tests
{
    /// <summary>
    /// Guards the seams that used to hardcode Windows paths, so the worker keeps
    /// running on both the Windows GPU host and the Linux container image.
    /// </summary>
    public class PortabilityTests
    {
        [Theory]
        [InlineData("Content", "trump.png")]
        [InlineData("Python", "moderation.py")]
        [InlineData("temp", "job.tmp")]
        public void WorkerPathsUseThePlatformSeparator(string directory, string file)
        {
            var path = directory switch
            {
                "Content" => WorkerPaths.Content("trump", ".png"),
                "Python" => WorkerPaths.Python(file),
                _ => WorkerPaths.Temp(file)
            };

            // Path.Combine, not a literal separator: on Linux the old "\\" spelling
            // produced one filename with backslashes in it rather than a path.
            Assert.Equal(Path.Combine(WorkerPaths.Root, directory, file), path);
        }

        [Fact]
        public void WorkerPathsCreatesTheTempDirectory()
        {
            var path = WorkerPaths.Temp($"{Guid.NewGuid()}.tmp");

            Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
        }

        [Fact]
        public void FontCatalogDefaultsToTheRunningPlatform()
        {
            var defaults = FontCatalog.Defaults;

            Assert.NotEmpty(defaults);
            Assert.All(defaults, font => Assert.Equal(
                OperatingSystem.IsWindows(), font.StartsWith("C:\\", StringComparison.Ordinal)));
        }

        [Fact]
        public void FontCatalogPrefersConfiguredFonts()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{FontCatalog.ConfigurationSection}:0"] = "/fonts/first.ttf",
                    [$"{FontCatalog.ConfigurationSection}:1"] = "/fonts/second.ttf"
                })
                .Build();

            Assert.Equal(new[] { "/fonts/first.ttf", "/fonts/second.ttf" },
                FontCatalog.Resolve(configuration).ToArray());
        }

        [Fact]
        public void FontCatalogFallsBackWhenNothingIsConfigured()
        {
            var configuration = new ConfigurationBuilder().Build();

            Assert.Equal(FontCatalog.Defaults, FontCatalog.Resolve(configuration));
        }

        [Fact]
        public void GetBestFontSkipsFontsThatAreNotInstalled()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{FontCatalog.ConfigurationSection}:0"] = "/nonexistent/missing.ttf"
                })
                .Build();

            var handler = new MagickImageHandler(new OpenCVHandler(), configuration);

            // No usable font is a fallback to ImageMagick's default, not a
            // failed job — which is what an unreadable font file used to be.
            Assert.Equal(string.Empty, handler.GetBestFont("hello"));
        }

        [Fact]
        public void HaarCascadeDefaultsToThePlatformOpenCvLocation()
        {
            Assert.Equal(
                OperatingSystem.IsWindows(),
                OpenCVHandler.DefaultHaarCascade.StartsWith("C:\\", StringComparison.Ordinal));
        }
    }
}
