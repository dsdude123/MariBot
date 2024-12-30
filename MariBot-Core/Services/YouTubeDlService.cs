using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace MariBot.Core.Services
{
    public class YouTubeDlService
    {
        private YoutubeDL client;

        public YouTubeDlService()
        {
            YoutubeDLSharp.Utils.DownloadYtDlp();
            YoutubeDLSharp.Utils.DownloadFFmpeg();
            client = new YoutubeDL();
            client.OutputFolder = "D:\\inetpub\\wwwroot\\transfer\\temp";
        }


        public async Task<RunResult<string>> DownloadVideo(string url)
        {
            var res = await client.RunVideoDownload(url: url, mergeFormat: DownloadMergeFormat.Mp4);
            return res;
        }
    }
}
