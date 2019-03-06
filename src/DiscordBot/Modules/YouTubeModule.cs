using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarBot.Services;
using System.Diagnostics;
using Discord.Audio;
using Discord;
using System.IO;
using System.Security.Authentication.ExtendedProtection.Configuration;
using Newtonsoft.Json;
using StarBot.Models;

namespace StarBot.Modules
{
    public class YouTubeModule : ModuleBase<SocketCommandContext>
    {
        public YouTubeService YouTubeService { get; set; }
        public static Dictionary<string, Queue<string>> queues = new Dictionary<string, Queue<string>>();
        public static Dictionary<string, bool> audiolocks = new Dictionary<string, bool>();

        [Command("yt-d", RunMode = RunMode.Async)]
        public Task ytd([Remainder] string url)
        {
            try
            {
                List<int> duration = YouTubeService.getDuration(url);
                string respond = "";
                for (int i = 0; i < duration.Count; i++)
                {
                    respond = respond + duration[i];
                    if (i + 1 < duration.Count)
                    {
                        respond = respond + ":";
                    }
                }
                return ReplyAsync(respond);
            } catch (Exception e)
            {
                return ReplyAsync(e.Message);
            }
        }

        [Command("yt", RunMode = RunMode.Async)]
        public async Task yt([Remainder] string url)
        {
            var channel = (Context.Message.Author as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You must be in a voice channel to use this command!");
                return;
            }

            if (url.Equals("skip")) // redirection for skip command
            {
                await ytskip();
                return;
            }

            // get guild and video id
            var guild = Context.Guild.Id.ToString();
            string videoid = YouTubeService.getID(url);
            videoid = videoid.Remove(videoid.Length - 1);


            // get youtube video store
            bool saveAtEnd = false;
            YouTubeStore myStore;
            if (System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json"))
            {
                myStore = JsonConvert.DeserializeObject<YouTubeStore>(
                    System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json"));
            }
            else
            {
                myStore = new YouTubeStore();
                myStore.cache = new Dictionary<string, YouTubeObject>();
                saveAtEnd = true;
            }

            YouTubeObject selectedVideo;

            if (queues.ContainsKey(guild)) // check if a queue has already been made for this session
            {
                List<int> duration;
                string name;
                if (myStore.cache.ContainsKey(videoid)) // check if our video store has the video so we can save time
                {
                    name = myStore.cache[videoid].name;
                    duration = myStore.cache[videoid].duration;
                }
                else // not in video store
                {
                    name = YouTubeService.getName(url).TrimEnd('\n');
                    duration = YouTubeService.getDuration(url);
                    YouTubeObject newVideo = new YouTubeObject();
                    newVideo.name = name;
                    newVideo.duration = duration;
                    myStore.cache.Add(videoid,newVideo);
                    saveAtEnd = true;
                }
                
                if (duration.Count > 2)
                {
                    ReplyAsync("Videos longer than 7 minutes are prohibited!");
                    if (saveAtEnd)
                    {
                        System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json",JsonConvert.SerializeObject(myStore));
                    }
                    return;
                }

                if (duration.Count == 2)
                {
                    if (duration[0] > 7 && duration[1] > 0)
                    {
                        ReplyAsync("Videos longer than 7 minutes are prohibited!");
                        if (saveAtEnd)
                        {
                            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json", JsonConvert.SerializeObject(myStore));
                        }
                        return;
                    }
                }
                queues[guild].Enqueue(url);
                ReplyAsync("Added `" + name + "` to queue!");
            } else
            {
                List<int> duration;
                string name;
                if (myStore.cache.ContainsKey(videoid))
                {
                    name = myStore.cache[videoid].name;
                    duration = myStore.cache[videoid].duration;
                }
                else
                {
                    name = YouTubeService.getName(url).TrimEnd('\n');
                    duration = YouTubeService.getDuration(url);
                    YouTubeObject newVideo = new YouTubeObject();
                    newVideo.name = name;
                    newVideo.duration = duration;
                    myStore.cache.Add(videoid, newVideo);
                    saveAtEnd = true;
                }
                queues[guild] = new Queue<string>();
                if (duration.Count > 2)
                {
                    ReplyAsync("Videos longer than 7 minutes are prohibited!");
                    if (saveAtEnd)
                    {
                        System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json", JsonConvert.SerializeObject(myStore));
                    }
                    return;
                }
                if (duration[0] > 7 && duration[1] > 0)
                {
                    ReplyAsync("Videos longer than 7 minutes are prohibited!");
                    if (saveAtEnd)
                    {
                        System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json", JsonConvert.SerializeObject(myStore));
                    }
                    return;
                }
                queues[guild].Enqueue(url);
                ReplyAsync("Added `" + name + "` to queue!");
            }
            if (audiolocks.ContainsKey(guild)) // check if we are actively playing a video
            {
                if (audiolocks[guild].Equals(true))
                {
                    if (saveAtEnd)
                    {
                        System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json", JsonConvert.SerializeObject(myStore));
                    }
                    return;
                }
                else
                {
                    audiolocks[guild] = true;
                }
            } else
            {
                audiolocks[guild] = true;
            }

            try
            {
                if (saveAtEnd)
                {
                    System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\store.json", JsonConvert.SerializeObject(myStore));
                }
                var audioClient = await channel.ConnectAsync();
                while (queues[guild].Count > 0)
                {
                    myStore = JsonConvert.DeserializeObject<YouTubeStore>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\" + guild +
                                                   "\\store.json"));
                    var i = queues[guild].Dequeue();
                    videoid = YouTubeService.getID(i).TrimEnd('\n');
                    var cachestatus = setupCache(Context.Guild.Id.ToString());
                    if (!File.Exists(Environment.CurrentDirectory + "\\cache\\" + guild + "\\" + videoid + ".mp3"))
                    {
                        var status = YouTubeService.getVideo(i, videoid, guild);
                    }
                    await ReplyAsync("Now playing: `" + myStore.cache[videoid].name + "`");
                    await SendAsync(audioClient, Environment.CurrentDirectory + "\\cache\\" + guild + "\\" + videoid + ".mp3");
                }
                audiolocks[guild] = false;
                audioClient.StopAsync();
            }
            catch (Exception e)
            {
                audiolocks[guild] = false;
                await ReplyAsync(e.Message);
            }

        }

        [Command("ytskip", RunMode = RunMode.Async)]
        public async Task ytskip()
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
                audiolocks[guild] = true;
                while (queues[guild].Count > 0)
                {
                    YouTubeStore myStore = JsonConvert.DeserializeObject<YouTubeStore>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\" + guild +
                                                   "\\store.json"));
                    var i = queues[guild].Dequeue();
                    var id = YouTubeService.getID(i);
                    id = id.Remove(id.Length - 1);
                    var cachestatus = setupCache(Context.Guild.Id.ToString());
                    if (!File.Exists(Environment.CurrentDirectory + "\\cache\\" + guild + "\\" + id + ".mp3"))
                    {
                        var status = YouTubeService.getVideo(i, id, guild);
                    }
                    YouTubeObject selectedVideo = myStore.cache[id];
                    await ReplyAsync("Now playing: `" + selectedVideo.name + "`");
                    await SendAsync(audioClient, Environment.CurrentDirectory + "\\cache\\" + guild + "\\" + id + ".mp3");
                }
                audiolocks[guild] = false;
                audioClient.StopAsync();
            }
            catch (Exception e)
            {
                audiolocks[guild] = false;
                await ReplyAsync(e.Message);
            }
        }

        private bool setupCache(string guild)
        {
            if(!Directory.Exists(Environment.CurrentDirectory + "\\cache\\" + guild))
            {
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\cache\\" + guild);
            }
            return true;
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
    }
}
