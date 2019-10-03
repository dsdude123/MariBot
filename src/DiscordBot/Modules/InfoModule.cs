using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using StarBot.Modules;
using StarBot.Services;

namespace DiscordBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        [Command("help")]
        public Task help()
        {
            var output = "**Help file for common commands**\n\n";
            output += "**info** - Displays info text.\n";
            output +=
                "**tts <text>** - Speaks a string in the voice channel you currently are in using DecTalk. Replace <text> with your string.\n";
            output += "**comic <text>** - Displays a specified comic. Use `comic help` for more info.\n";
            output += "**webcam <text>** - Displays a specified webcam. Use `webcam help` for more info.\n";
            output +=
                "**urban <text>** - Displays a random Urban Dictionary definition for the specified word. Replace <text> with your word.\n";
            output +=
                "**urbantop <text>** - Displays the top Urban Dictionary definition for the specified word. Replace <text> with your word.\n";
            output += "**urbanrand** - Gets a random word on Urban Dictionary and displays its definition.\n";
            output +=
                "**wiki <text>** - Gets the specified Wikipedia article and displays the first section. The first result from a search will be used. Replace <text> with article name or search term.\n";
            output +=
                "**wikisearch <text>** - Performs a search on Wikipedia and returns the result. Replace <text> with your search term.\n";
            output += "**flipcoin** - Flips a coin.\n";
            output += "**radar** - **EXPERIEMENTAL** Gets the current radar animation for the Pacific Northwest.\n";
            output +=
                "**radio <text>** - Plays a specified radio station in the voice channel you currently are in. Use `radio help` for more info.\n";
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            return ReplyAsync("", false, eb);
        }

        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 1.0\n");

        [Command("tts")]
        public Task tts([Remainder]string text)
        {
            CreateTTS(text);
            var sender = Context.Message.Author.Id;
            var user = Context.Client.CurrentUser;
            return ReplyAsync($"{user.Mention} executetts {sender}");
        }

        [Command("executetts",RunMode = RunMode.Async)]
        private async Task executeTTS([Remainder] string org)
        {
            
            var channel = (Context.Guild.GetUser(ulong.Parse(org)))?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, Environment.CurrentDirectory + "\\tts.wav");
            audioClient.StopAsync();
        }

        [Command("radar")]
        public async Task radar()
        {
            var image = PictureService.GetPictureAsync("http://images.intellicast.com/WxImages/RadarLoop/tiw_None_anim.gif").Result;
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "radar.gif");
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
            return Process.Start(new ProcessStartInfo
            {
                FileName = "SharpTalkGenerator",
                Arguments = $"{text}",
                UseShellExecute = false,
                RedirectStandardOutput = false,
            });
        }
    }
}
