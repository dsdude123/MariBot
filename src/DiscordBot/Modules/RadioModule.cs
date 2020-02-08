using Discord;
using Discord.Audio;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Modules
{
    [Group("radio")]
    public class RadioModule : ModuleBase<SocketCommandContext>
    {

        public static Dictionary<Process,string> activeradios = new Dictionary<Process, string>();
        [Command("help")]
        public Task help()
        {
            var output = "**Help file for radio commands**\n\n";
            output += "All commands are performed in the voice channel you are currently in. Use **audio stop** to stop the audio.\n\n";
            output += "**radio noaa_spokane** - Plays NOAA Weather Radio for the Spokane area.\n";
            output += "**radio knhc** - Plays C89.5 (KNHC Seattle).\n";
            output += "**radio kexp** - Plays KEXP (Seattle).\n";
            output += "**radio kxsu** - Plays KXSU (SeattleU)\n";
            output += "**radio uwave** - Plays UWave radio (UW Bothell).\n";
            output += "**radio kisw** - Plays 99.9 The Rock (KISW Seattle).\n";
            output += "**radio kndd** - Plays 100.7 The End (KNDD Seattle).\n";
            output += "**radio kqmv** - Plays MOViN 92.5 (KQMV Seattle).\n";
            output += "**radio kube** - Plays KUBE 93.3 (Seattle).\n";
            output += "**radio kswd** - Plays 94.1 The sound (KSWD Seattle).\n";
            output += "**radio trij** - Plays Triple J (Australia).\n";
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            return ReplyAsync("", false, eb.Build());
        }

        [Command("noaa_spokane", RunMode = RunMode.Async)]
        public async Task noaaspokane()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "http://wxradio.grimtech.net:8000/KE7NWL/Spokane.mp3", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("trij", RunMode = RunMode.Async)]
        public async Task trij()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if(channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "http://live-radio01.mediahubaustralia.com/2TJW/mp3/", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }



        [Command("knhc", RunMode = RunMode.Async)]
        public async Task knhc()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "http://knhc-ice.streamguys1.com:80/live", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kxsu", RunMode = RunMode.Async)]
        public async Task kxsu()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "http://lin1.san.fast-serv.com:6014/stream?1541383056872", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kexp", RunMode = RunMode.Async)]
        public async Task kexp()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://kexp-mp3-128.streamguys1.com/kexp128.mp3", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("uwave", RunMode = RunMode.Async)]
        public async Task uwave()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://live.uwave.fm:8443/listen-128.mp3", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kisw", RunMode = RunMode.Async)]
        public async Task kisw()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://19813.live.streamtheworld.com/KISWFMAAC.aac", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kndd", RunMode = RunMode.Async)]
        public async Task kndd()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://18313.live.streamtheworld.com/KNDDFMAAC.aac", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kqmv", RunMode = RunMode.Async)]
        public async Task kqmv()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://19493.live.streamtheworld.com/KQMVFMAAC.aac", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kube", RunMode = RunMode.Async)]
        public async Task kube()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, "https://c1.prod.playlists.ihrhls.com/2577/playlist.m3u8", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("kswd", RunMode = RunMode.Async)]
        public async Task kswd()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, " https://17933.live.streamtheworld.com/KSWDFMAAC.aac", Context.Guild.Id.ToString());
            audioClient.StopAsync();
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task stop()
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            List<Process> toRemove = new List<Process>();
            foreach (var radio in activeradios)
            {
                if (radio.Value.Equals(Context.Guild.Id.ToString()))
                {
                   radio.Key.Kill();
                   toRemove.Add(radio.Key);
                }
            }

            foreach (var radio in toRemove)
            {
                activeradios.Remove(radio);
            }
            audioClient.StopAsync();
        }
       
        private async Task SendAsync(IAudioClient client, string path, string guild)
        {
            // Create FFmpeg using the previous example
            var ffmpeg = CreateStream(path);
            activeradios.Add(ffmpeg,guild);
            using (ffmpeg)
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Music,24000,10000))
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
    }
}
