using CSharpMath.SkiaSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class PictureService
    {
        private readonly HttpClient _http;

        public PictureService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetPictureAsync(string URL)
        {
            var resp = await _http.GetAsync(URL);
            return await resp.Content.ReadAsStreamAsync();
        }

        // TODO: Add support for changing color output (.TextColor)
        public async Task<Stream> GetLatexImage(String latex)
        {
            var painter = new MathPainter();
            painter.LaTeX = latex;
            return painter.DrawAsStream(format: SKEncodedImageFormat.Png);
        }
    }
}
   