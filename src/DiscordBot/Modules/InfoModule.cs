using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using ImageMagick;
using MariBot.Modules;
using MariBot.Services;

namespace DiscordBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        ///     The Biohazard symbol emoji.
        /// </summary>
        private readonly string BIOHAZARD = "\u2623";

        public PictureService PictureService { get; set; }

        public DiscordSocketClient DiscordClient { get; set; }

        public InfoModule()
        {
            DiscordClient.ReactionAdded += ReactionAddedHandlerAsync;
        }

        [Command("help")]
        public Task help()
        {
            if (Context.Guild.Id == 297485054836342786) // Server is prohibited from using some commands
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/297485054836342786/commands.html");
            }
            else
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/commands.html");
            }
        }

        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 1.0\n"); // TODO: Fix version

        [Command("tts", RunMode = RunMode.Async)]
        public async Task tts([Remainder]string text)
        {
            try
            {
                CreateTTS(text).WaitForExit();
            } catch (FileNotFoundException e)
            {
                await Context.Channel.SendMessageAsync("SharpTalkGenerator is not installed.");
                return;
            }
            var channel = (Context.Guild.GetUser(Context.Message.Author.Id))?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendFileAsync(Environment.CurrentDirectory + "\\tts.wav");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, Environment.CurrentDirectory + "\\tts.wav");
            audioClient.StopAsync();
        }

        [Command("radar")]
        public async Task radar()
        {
            var image = PictureService.GetPictureAsync("https://s.w-x.co/staticmaps/wu/wxtype/county_loc/tiw/animate.png").Result;
            image.Seek(0, SeekOrigin.Begin);
            MemoryStream outgoingImage = new MemoryStream();

            using (var baseImage = new MagickImageCollection(image))
            {
                using (var outputCollection = new MagickImageCollection())
                {
                    bool usedBaseOnce = false;
                    baseImage.Coalesce();
                    foreach(var frame in baseImage)
                    {
                        outputCollection.Add(new MagickImage(frame));
                    }
                    outputCollection.Write(outgoingImage, MagickFormat.Gif);
                } 
            }
            outgoingImage.Seek(0, SeekOrigin.Begin);
            Context.Channel.SendFileAsync(outgoingImage, "radar.gif");
        }

        [Command("latex")]
        public async Task latex([Remainder] string latex)
        {
            var image = PictureService.GetLatexImage(latex).Result;
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "latex.png");
        }

        [Command("solve")]
        public Task solve([Remainder] string equation)
        {
            org.mariuszgromada.math.mxparser.Expression expression = new org.mariuszgromada.math.mxparser.Expression(equation);
            return Context.Channel.SendMessageAsync(expression.calculate().ToString());
        }

        [Command("waifu")]
        public async Task waifu()
        {
            string id = Guid.NewGuid().ToString();
            string filePath = Environment.CurrentDirectory + $"\\cache\\{Context.Guild.Id}\\{id}.png";
            Directory.CreateDirectory(Environment.CurrentDirectory + $"\\cache\\{Context.Guild.Id}");

            Process waifulabs = CreateWaifu(filePath);
            waifulabs.WaitForExit();
            if(File.Exists(filePath))
            {
                await Context.Channel.SendFileAsync(filePath);
            } else
            {
                await Context.Channel.SendMessageAsync("Failed to generate waifu.");
            }
        }

        [Command("uwu")]
        public async Task UwuifyAsync([Remainder] string input = null)
        {
            if(input == null)
            {
                input = (await Context.Channel.GetMessagesAsync(2, Discord.CacheMode.AllowDownload)
                    .FlattenAsync())
                    .ToArray()
                    .OrderBy(message => message.Timestamp)
                    .First().Content;
            }
            input = UwuifyText(input);
            
            await Context.Channel.SendMessageAsync(input);
        }

        private string UwuifyText(string input)
        {
            input = Regex.Replace(input, "(?:r|l)", "w");
            input = Regex.Replace(input, "(?:R|L)", "W");
            input = Regex.Replace(input, "n([aeiou])", "ny$1");
            input = Regex.Replace(input, "N([aeiou])", "Ny$1");
            input = Regex.Replace(input, "N([AEIOU])", "NY$1");
            input = Regex.Replace(input, "ove", "uv");
            return input;
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Process CreateTTS(string text)
        {
            if(!File.Exists("SharpTalkGenerator.exe"))
            {
                throw new FileNotFoundException("SharpTalk is not installed.");
            }
            return Process.Start(new ProcessStartInfo
            {
                FileName = "SharpTalkGenerator",
                Arguments = $"{text}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Process CreateWaifu(string path)
        {
            if (!File.Exists("waifulabs.exe"))
            {
                throw new FileNotFoundException("WaifuLabs.NET is not installed.");
            }
            return Process.Start(new ProcessStartInfo
            {
                FileName = "waifulabs",
                Arguments = $"{path}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private async Task ReactionAddedHandlerAsync(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == BIOHAZARD)
            {
                var message = await userMessage.GetOrDownloadAsync();
                var text = UwuifyText(message.Content);
                await channel.SendMessageAsync(text);
            }
        }
    }
}
