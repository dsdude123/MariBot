using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class GoogleService
    {
        private CustomsearchService searchService;

        public GoogleService()
        {
            searchService = new CustomsearchService(new BaseClientService.Initializer
            {
                ApplicationName = "MariBot",
                ApiKey = DiscordBot.Program._config["googlecloudkey"]
            });
        }

        public async Task<Search> Search(string keyword, bool imageSearch = false) {
            CseResource.ListRequest searchRequest = new CseResource.ListRequest(searchService);
            searchRequest.Cx = DiscordBot.Program._config["googlecustomsearchid"];
            searchRequest.Q = keyword;
            searchRequest.Safe = CseResource.ListRequest.SafeEnum.Active;
            if(imageSearch)
            {
                searchRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
            }

            return searchRequest.ExecuteAsync().Result;
        }

    }
}
