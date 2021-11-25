using Discord.Commands;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    /**
     * Edges2HentaiService
     * 
     * Resource Intensive - Not Usable in Unpermitted Servers
     * 
     * Takes an image and turns it into a weird flesh colored blob (although sometimes not flesh colored).
     */
    public class Edges2HentaiService
    {

        public PictureService pictureService = new PictureService(new System.Net.Http.HttpClient());

        public async void Edges2Hentai(SocketCommandContext context, string url)
        {
            try
            {
                url = pictureService.GetImageUrl(context, url);
            }
            catch (NotSupportedException e)
            {
                await context.Channel.SendMessageAsync(e.Message);
                return;
            }

            MemoryStream incomingImage = pictureService.GetWebResource(url).Result;
            incomingImage.Seek(0, SeekOrigin.Begin);

            Guid requestId = Guid.NewGuid();
            MagickImage inputFile = new MagickImage(incomingImage);
            FileStream savedFile = new FileStream($".\\edges2hentai\\input-{requestId}.png", FileMode.CreateNew);
            inputFile.Write(savedFile, MagickFormat.Png);
            savedFile.Close();
            inputFile.Dispose();
            
            Process generator = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                //Arguments = $"run .\\edges2hentai.py --input_path .\\input-{requestId}.png --output_path .\\{requestId}.png",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = ".\\edges2hentai"
            });

            using (var sw = generator.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("conda activate");
                    sw.WriteLine($"python .\\edges2hentai.py --input_path .\\input-{requestId}.png --output_path .\\{requestId}.png");
                }
            }

            generator.WaitForExit();

            await context.Channel.SendFileAsync($".\\edges2hentai\\{requestId}.png");

            File.Delete($".\\edges2hentai\\input-{requestId}.png");
            File.Delete($".\\edges2hentai\\{requestId}.png");
        }
    }
}
