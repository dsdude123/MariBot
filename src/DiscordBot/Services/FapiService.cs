using Newtonsoft.Json;
using MariBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
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

        public async Task<Stream> ExecuteImageRequest(string function, FapiRequest body)
        {
            string json = JsonConvert.SerializeObject(body);
            HttpContent request = new StringContent(json);
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_TOKEN);
            HttpResponseMessage response = httpClient.PostAsync(BuildUrl(function), request).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStreamAsync().Result;
        }

        public async Task<String> ExecuteTextRequest(string function, FapiRequest body)
        {
            string json = JsonConvert.SerializeObject(body);
            HttpContent request = new StringContent(json);
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_TOKEN);
            HttpResponseMessage response = httpClient.PostAsync(BuildUrl(function), request).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
