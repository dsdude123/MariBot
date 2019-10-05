using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarBot.Services;
using System.Diagnostics;
using Discord.Audio;
using Discord;
using System.IO;
using Newtonsoft.Json;
using StarBot.Models;
using System.Timers;

namespace StarBot.Modules
{
    public class YouTubeModule : ModuleBase<SocketCommandContext>
    {
        public static readonly int SAVE_INTERVAL_MINUTES = 30;

        public YouTubeService YouTubeService { get; set; }
        public static Dictionary<string, Queue<string>> queues = new Dictionary<string, Queue<string>>();
        public static Dictionary<string, bool> audioLocks = new Dictionary<string, bool>();
        public static YouTubeStore videoDatabase;
        public static Boolean datbaseChanged = false;
        public static System.Timers.Timer saveTimer;
        public static Boolean firstStartup = true;

        public YouTubeModule()
        {
            if (firstStartup)
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "\\cache"))
                {
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\cache");
                    videoDatabase = new YouTubeStore();
                    videoDatabase.cache = new Dictionary<string, YouTubeObject>();
                }
                else
                {
                    if (System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\ytstore.json"))
                    {
                        videoDatabase = JsonConvert.DeserializeObject<YouTubeStore>(
                            System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\ytstore.json"));
                    }
                    else
                    {
                        videoDatabase = new YouTubeStore();
                        videoDatabase.cache = new Dictionary<string, YouTubeObject>();
                    }
                }

                saveTimer = new System.Timers.Timer(SAVE_INTERVAL_MINUTES * 60 * 1000);
                saveTimer.Elapsed += saveDatabase;
                saveTimer.AutoReset = true;
                saveTimer.Enabled = true;
                firstStartup = false;
            }
        }

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
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }

            if (videoUrl.Equals("skip")) // redirection for skip command
            {
                await SkipVideo();
                return;
            }

            // get guild and video id
            var guild = Context.Guild.Id.ToString();
            string videoid = YouTubeService.getID(videoUrl);
            videoid = videoid.Remove(videoid.Length - 1);

            if (!queues.ContainsKey(guild)) // check if a queue has already been made for this session
            {
                queues[guild] = new Queue<string>();
            }

            List<int> duration;
            string name;
            if (videoDatabase.cache.ContainsKey(videoid)) // check if our video store has the video so we can save time
            {
                name = videoDatabase.cache[videoid].name;
                duration = videoDatabase.cache[videoid].duration;
            }
            else // not in video store
            {
                name = YouTubeService.getName(videoUrl).TrimEnd('\n');
                duration = YouTubeService.getDuration(videoUrl);
                YouTubeObject newVideo = new YouTubeObject();
                newVideo.name = name;
                newVideo.duration = duration;
                videoDatabase.cache.Add(videoid, newVideo);
                datbaseChanged = true;
            }
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

            queues[guild].Enqueue(videoUrl);
            ReplyAsync("Added `" + name + "` to queue!");

            if (audioLocks.ContainsKey(guild) && audioLocks[guild].Equals(true)) // check if we are actively playing a video
            {
                return;
            }

            audioLocks[guild] = true;

            try
            {
                var audioClient = await channel.ConnectAsync();
                while (queues[guild].Count > 0)
                {
                    var nextVideoUrl = queues[guild].Dequeue();
                    videoid = YouTubeService.getID(nextVideoUrl).TrimEnd('\n');
                    if (!File.Exists(Environment.CurrentDirectory + "\\cache\\" + videoid + ".mp3"))
                    {
                        var status = YouTubeService.getVideo(nextVideoUrl, videoid, guild);
                    }
                    await ReplyAsync("Now playing: `" + videoDatabase.cache[videoid].name + "`");
                    await PlayAudio(audioClient, Environment.CurrentDirectory + "\\cache\\" + videoid + ".mp3");
                }
                audioLocks[guild] = false;
                audioClient.StopAsync();
            }
            catch (Exception e)
            {
                audioLocks[guild] = false;
                await ReplyAsync(e.Message);
            }

        }

        [Command("ytskip", RunMode = RunMode.Async)]
        public async Task SkipVideo()
        {
            var guild = Context.Guild.Id.ToString();
            try
            {
                var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
                if (channel == null)
                {
                    await ReplyAsync("You must be in a voice channel to use this command!");
                    return;
                }
                var audioClient = await channel.ConnectAsync();
                audioLocks[guild] = true;
                while (queues[guild].Count > 0)
                {
                    var nextVideoUrl = queues[guild].Dequeue();
                    var videoId = YouTubeService.getID(nextVideoUrl);
                    videoId = videoId.TrimEnd('\n');
                    if (!File.Exists(Environment.CurrentDirectory + "\\cache\\" + videoId + ".mp3"))
                    {
                        var status = YouTubeService.getVideo(nextVideoUrl, videoId, guild);
                    }
                    YouTubeObject selectedVideo = videoDatabase.cache[videoId];
                    await ReplyAsync("Now playing: `" + selectedVideo.name + "`");
                    await PlayAudio(audioClient, Environment.CurrentDirectory + "\\cache\\" + videoId + ".mp3");
                }
                audioLocks[guild] = false;
                audioClient.StopAsync();
            }
            catch (Exception e)
            {
                audioLocks[guild] = false;
                await ReplyAsync(e.Message);
            }
        }

        private void saveDatabase(Object source, ElapsedEventArgs e)
        {

            if (datbaseChanged)
            {
                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\ytstore.json", JsonConvert.SerializeObject(videoDatabase));
                datbaseChanged = false;
            }
        }

        private async Task PlayAudio(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateAudioSource(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateAudioSource(string path)
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
