using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediawikiSharp_API;
using WikipediaNET;


namespace StarBot.Services
{
    public class WikipediaService
    {
        private MediawikiSharp_API.Mediawiki Wikipedia;
        public WikipediaService(MediawikiSharp_API.Mediawiki uc)
            => Wikipedia = uc;

        public async Task<WikipediaObject> GetWikipediaPage(string search)
        {
            WikipediaNET.Wikipedia wiki = new Wikipedia();
            WikipediaNET.Objects.QueryResult results = wiki.Search(search);
            if (results == null || results.Search == null || results.Search.Count == 0)
            {
                throw new Exception("Could not find an article similar to that name.");
            }

            string toGet = "";
            if (results.Search[0].RedirectTitle == null)
            {
                toGet = results.Search[0].Title;
            }
            else
            {
                toGet = results.Search[0].RedirectTitle;
            }

            Wikipedia = new Mediawiki("en");
            
            var sections = Wikipedia.GetPageSections(toGet);
            if (sections == null || sections.Count == 0)
            {
                throw new Exception("An error occured loading the page.");
            }

            WikipediaObject returnval = new WikipediaObject();
            returnval.title = toGet;
            returnval.text = sections[0].Content;
            return returnval;
        }

        public async Task<List<string>> GetWikipediaResults(string search)
        {
            WikipediaNET.Wikipedia wiki = new Wikipedia();
            WikipediaNET.Objects.QueryResult results = wiki.Search(search);
            if (results == null || results.Search == null || results.Search.Count == 0)
            {
                throw new Exception("Could not find an article similar to that name.");
            }

            List<string> returnval = new List<string>();

            for (int i = 0; i < results.Search.Count; i++)
            {
                string toGet = "";
                if (results.Search[i].RedirectTitle == null)
                {
                    toGet = results.Search[i].Title;
                }
                else
                {
                    toGet = results.Search[i].RedirectTitle;
                }
                returnval.Add(toGet);
            }

            return returnval;
        }

        public class WikipediaObject
        {
            public string title;
            public string text;
        }

    }
}
