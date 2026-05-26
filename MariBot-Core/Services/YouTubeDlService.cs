using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace MariBot.Core.Services
{
    public class YouTubeDlService
    {
        private YoutubeDL client;

        public YouTubeDlService(IConfiguration configuration)
        {
            YoutubeDLSharp.Utils.DownloadYtDlp();
            YoutubeDLSharp.Utils.DownloadFFmpeg();
            client = new YoutubeDL();
            client.OutputFolder = configuration.GetValue<string>("DiscordSettings:YouTubeDlOutputPath")
                ?? Path.Combine(Path.GetTempPath(), "maribot-yt");
            Directory.CreateDirectory(client.OutputFolder);
        }


        public async Task<RunResult<string>> DownloadVideo(string url)
        {
            var res = await client.RunVideoDownload(url: url, mergeFormat: DownloadMergeFormat.Mp4);
            return res;
        }
    }
}
