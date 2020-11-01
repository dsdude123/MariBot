using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using MariBot.Modules;
using MariBot.Services;

namespace DiscordBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        [Command("help")]
        public Task help()
        {
            return ReplyAsync("https://dsdude123.github.io/MariBot/commands.html");
        }

        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net 1.0\n");

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
            if(!DiscordBot.Program.isSharpTalkPresent)
            {
                throw new FileNotFoundException("SharpTalk is not installed.");
            }
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
