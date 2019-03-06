using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;

namespace StarBot.Services
{
    /*
     * Why the hell am I writing this?
     */
    public class Rule34Service
    {
        private readonly HttpClient _http;

        public Rule34Service(HttpClient http)
            => _http = http;

        public async Task<Stream> GetDataAsync(string URL)
        {
            var resp = await _http.GetAsync(URL);
            return await resp.Content.ReadAsStreamAsync();
        }

        public const string API_URL = "https://rule34.xxx/index.php?page=dapi&s=post&q=index";
        public const int RESULT_LIMIT = 100;

        public async Task<XmlDocument> GetAPIResponse(string request)
        {
            Stream xml = await GetDataAsync(request);
            XmlDocument myDoc = new XmlDocument();
            myDoc.Load(xml);
            return myDoc;
        }
        public async Task<String> GenerateURL(string tags = null, int pageNumber = 0, bool nsfw = true)
        {
            string generatedString = API_URL;
            if (pageNumber > 2000 || tags == null)
            {
                throw new NotSupportedException();
            }

            generatedString += "&pid=" + pageNumber;
            generatedString += "&limit=" + RESULT_LIMIT;
            tags.Replace(' ', '+');
            generatedString += "&tags=" + tags;
            if (nsfw)
            {
                generatedString += "&rating:explicit";
            }

            return generatedString;
        }

        public async Task<int> GetTotalImages(XmlDocument response)
        {
            return int.Parse(response["posts"].Attributes["count"].Value);
        }

        public async Task<List<String>> GetImageUrls(XmlDocument response)
        {
            XmlNodeList files = response["posts"].ChildNodes;
            List<String> result = new List<string>();
            for (int i = 0; i < files.Count; i++)
            {
                result.Add(files[i].Attributes["file_url"].Value);
            }

            return result;
        }


    }
}
