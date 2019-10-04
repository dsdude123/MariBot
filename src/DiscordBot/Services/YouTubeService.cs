using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StarBot.Services
{
    public class YouTubeService
    { 
        public List<int> getDuration(string url)
        {
            List<int> result = new List<int>();
            Process yt = Execute("--get-duration --default-search ytsearch " + "\"" + url + "\"");
            yt.WaitForExit();
            String output = yt.StandardOutput.ReadToEnd();
            string[] split = output.Split(':');
            try
            {
                foreach (var i in split)
                {
                    result.Add(int.Parse(i));
                }
            } catch (Exception e)
            {
                throw new Exception("Failed to get video duration!");
            }
            return result;
        }

        public string getID(string url)
        {
            Process yt = Execute("--get-id --default-search ytsearch " + "\"" + url + "\"");
            yt.WaitForExit();
            String result = yt.StandardOutput.ReadToEnd();
            return result;
        }

        public string getName(string url)
        {
            Process yt = Execute("--get-title --default-search ytsearch " + "\"" + url + "\"");
            yt.WaitForExit();
            String result = yt.StandardOutput.ReadToEnd();
            return result;
        }

        public bool getVideo(string url, string id, string guild)
        {
            try
            {
                Process yt = Execute($"--no-playlist --default-search ytsearch --output \"\\cache\\{id}.mp3\" -f best --extract-audio --audio-format mp3 " + "\"" + url + "\"");
                yt.WaitForExit();
                return true;
            } catch (Exception e)
            {
                throw new Exception("Failed to get video!");
            }
        }

        private Process Execute(string arg)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"{arg}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
