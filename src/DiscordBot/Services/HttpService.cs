using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class HttpService
    {
        private readonly HttpClient _http;

        public HttpService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetHttpAsync(string URL)
        {
            var resp = await _http.GetAsync(URL);
            return await resp.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GetHttpAsyncWithSpecialHeader(string URL, string host, string referrer)
        {
            string temp = _http.DefaultRequestHeaders.Host;
            Uri temp2 = _http.DefaultRequestHeaders.Referrer;
            _http.DefaultRequestHeaders.Host = host;
            _http.DefaultRequestHeaders.Referrer = new Uri(referrer);
            var resp = await _http.GetAsync(URL);
            _http.DefaultRequestHeaders.Host = temp;
            _http.DefaultRequestHeaders.Referrer = temp2;
            return await resp.Content.ReadAsStreamAsync();
        }
    }
}
