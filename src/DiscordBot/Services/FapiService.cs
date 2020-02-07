using Newtonsoft.Json;
using StarBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Services
{
    /*
     * Service for fAPI 
     * https://fapi.dreadful.tech/index.html
     */
    public class FapiService
    {
        private string API_TOKEN;
        private readonly string ENDPOINT = "https://fapi.wrmsr.io/";

        public FapiService()
        {
            API_TOKEN = DiscordBot.Program._config["fapiapitoken"];
        }

        public string BuildUrl(string function)
        {
            return ENDPOINT + function;
        }

        public HttpContent BuildImageRequest(FapiRequest body)
        {
            return BuildRequest(body, "image/png");
        }

        public HttpContent BuildTextRequest(FapiRequest body)
        {
            return BuildRequest(body, "text/plain");
        }

        public HttpContent BuildRequest(FapiRequest body, string contentType)
        {
            string json = JsonConvert.SerializeObject(body);
            HttpContent request = new StringContent(json);
            request.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            request.Headers.Add("Authorization", API_TOKEN);
            return request;
        }

        public async Task<Stream> ExecuteImageRequest(string function, HttpContent body)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.PostAsync(BuildUrl(function), body).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStreamAsync().Result;
        }

        public async Task<String> ExecuteTextRequest(string function, HttpContent body)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.PostAsync(BuildUrl(function), body).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
