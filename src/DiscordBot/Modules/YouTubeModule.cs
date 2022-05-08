using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MariBot.Services;
using System.Diagnostics;
using Discord.Audio;
using Discord;
using System.IO;
using Newtonsoft.Json;
using MariBot.Models;
using System.Timers;

namespace MariBot.Modules
{
    public class YouTubeModule : ModuleBase<SocketCommandContext>
    {
        public static readonly int SAVE_INTERVAL_MINUTES = 30;

        public YouTubeService YouTubeService { get; set; }
        public static Dictionary<string, Queue<Tuple<string,string>>> queues = new Dictionary<string, Queue<Tuple<string,string>>>(); // Queue of videos to play
        public static Dictionary<string, IAudioClient> guildClients = new Dictionary<string, IAudioClient>(); // Guild to client mapping
        public static Dictionary<IAudioClient, AudioOutStream> outboundStreams = new Dictionary<IAudioClient, AudioOutStream>(); // Active audio streams
        public static Dictionary<IAudioClient, Process> activeProcesses = new Dictionary<IAudioClient, Process>();


        [Command("yt-d", RunMode = RunMode.Async)]
        public Task GetVideoDuration([Remainder] string videoUrl)
        {
            try
            {
                List<int> duration = YouTubeService.getDuration(videoUrl);
                string response = "";
                for (int i = 0; i < duration.Count; i++)
                {
                    response = response + duration[i];
                    if (i + 1 < duration.Count)
                    {
                        response = response + ":";
                    }
                }
                return ReplyAsync(response);
            }
            catch (Exception e)
            {
                return ReplyAsync(e.Message);
            }
        }

        [Command("yt", RunMode = RunMode.Async)]
        public async Task PlayYoutubeVideo([Remainder] string videoUrl)
        {

            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            var guild = Context.Guild.Id.ToString();
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }

            if (videoUrl.Equals("skip") && guildClients.ContainsKey(guild))
            {
                var audioClient = guildClients[guild];
                if (activeProcesses.ContainsKey(audioClient))
                {
                    KillProcessTree(activeProcesses[audioClient]);
                }
                return;
            }

            // get video id
            string videoid = YouTubeService.getID(videoUrl);
            videoid = videoid.Remove(videoid.Length - 1);

            if (!queues.ContainsKey(guild)) // check if a queue has already been made for this session
            {
                queues[guild] = new Queue<Tuple<string,string>>();
            }

            List<int> duration = YouTubeService.getDuration(videoUrl);
            string name = YouTubeService.getName(videoUrl).TrimEnd('\n');

            if (duration.Count > 1)
            {

                if (duration.Count > 2)
                {
                    ReplyAsync("Videos longer than 7 minutes are prohibited!");
                }

                if (duration[0] > 7 && duration[1] > 0)
                {
                    ReplyAsync("Videos longer than 7 minutes are prohibited!");
                }
            }

            queues[guild].Enqueue(new Tuple<string, string>(name, videoUrl));
            ReplyAsync("Added `" + name + "` to queue!");

            if (guildClients.ContainsKey(guild)) // check if we are actively playing a video
            {
                return;
            }

            try
            {
                var audioClient = await channel.ConnectAsync();
                guildClients[guild] = audioClient;
                while (queues[guild].Count > 0)
                {
                    Tuple<string, string> nextVideo = queues[guild].Dequeue();
                    await ReplyAsync("Now playing: `" + nextVideo.Item1 + "`");
                    await PlayAudio(audioClient, nextVideo.Item2);
                }
                audioClient.StopAsync();
                guildClients.Remove(guild);
            }
            catch (Exception e)
            {
                guildClients.Remove(guild);
                await ReplyAsync(e.Message);
            }

        }

        private async Task PlayAudio(IAudioClient client, string url)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateAudioSource(url))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            {
                activeProcesses.Add(client, ffmpeg);
                if (outboundStreams.ContainsKey(client))
                {
                    try { await output.CopyToAsync(outboundStreams[client]); }
                    finally { await outboundStreams[client].FlushAsync(); }
                } else
                {
                    outboundStreams[client] = client.CreatePCMStream(AudioApplication.Mixed);
                    try { await output.CopyToAsync(outboundStreams[client]); }
                    finally { await outboundStreams[client].FlushAsync(); }
                }
                activeProcesses.Remove(client);
            }
        }

        private Process CreateAudioSource(string url)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // TODO: Make the initial search store the video link so we don't search again if the link isn't a url
                Arguments = $"/C youtube-dl --ignore-errors --default-search ytsearch -f bestaudio -o - \"{url}\" | ffmpeg -err_detect ignore_err -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private void KillProcessTree(Process process)
        {
            string taskkill = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe");
            using (var procKiller = new System.Diagnostics.Process())
            {
                procKiller.StartInfo.FileName = taskkill;
                procKiller.StartInfo.Arguments = string.Format("/PID {0} /T /F", process.Id);
                procKiller.StartInfo.CreateNoWindow = true;
                procKiller.StartInfo.UseShellExecute = false;
                procKiller.Start();
                procKiller.WaitForExit(1000);
            }
        }
    }
}
