using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Kgsearch.v1;
using Google.Apis.Kgsearch.v1.Data;
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
        private static readonly string ApiKey = MariBot.Program.config["googlecloudkey"];
        private static readonly string ApplicationName = "MariBot";

        private CustomsearchService SearchService;
        private KgsearchService KnowledgeGraphService;

        public GoogleService()
        {
            SearchService = new CustomsearchService(new BaseClientService.Initializer
            {
                ApplicationName = ApplicationName,
                ApiKey = ApiKey
            });
            KnowledgeGraphService = new KgsearchService(new BaseClientService.Initializer
            {
                ApplicationName = ApplicationName,
                ApiKey = ApiKey
            });
        }

        public async Task<Search> Search(string keyword, bool imageSearch = false) {
            CseResource.ListRequest searchRequest = new CseResource.ListRequest(SearchService);
            searchRequest.Cx = MariBot.Program.config["googlecustomsearchid"];
            searchRequest.Q = keyword;
            searchRequest.Safe = CseResource.ListRequest.SafeEnum.Active;
            if(imageSearch)
            {
                searchRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
            }

            return searchRequest.ExecuteAsync().Result;
        }

        public async Task<SearchResponse> KnowledgeGraph(string keyword)
        {
            EntitiesResource.SearchRequest searchRequest = new EntitiesResource.SearchRequest(KnowledgeGraphService);
            searchRequest.Query = keyword;
            searchRequest.Limit = 1;
            return searchRequest.ExecuteAsync().Result;
        }

    }
}
