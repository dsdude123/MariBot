using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediawikiSharp_API;
using WikipediaNET;


namespace MariBot.Services
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
            WikipediaObject returnval = new WikipediaObject();

            if (!hasResults(results))
            {
                if (results.SearchInfo.Suggestion != null)
                {
                    results = wiki.Search(results.SearchInfo.Suggestion);
                }
                else
                {
                    returnval.title = "Wikipedia returned no results for query: " + search;
                    return returnval;
                }
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

            returnval.title = toGet;
            returnval.text = sections[0].Content;

            List<ImageWiki> images = Wikipedia.GetImagesURL(toGet);
            images = removeProhibitedResults(images);
            if(hasImageResults(images))
            {
                returnval.imageURL = images[0].URL;
            }
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

        private bool hasResults(WikipediaNET.Objects.QueryResult results)
        {
            if (results == null || results.Search == null || results.Search.Count == 0)
            {
                return false;
            } else if (results.Search.Count == 1)
            {
                if(results.Search[0].Title == null && results.Search[0].RedirectTitle == null)
                {
                    return false;
                }
            }
            return true;
        }

        private bool hasImageResults(List<ImageWiki> images)
        {
            if(images == null || images.Count < 1 || images[0].URL == null)
            {
                return false;
            }
            return true;
        }

        private List<ImageWiki> removeProhibitedResults(List<ImageWiki> images)
        {
            List<ImageWiki> newImages = new List<ImageWiki>();
            foreach(ImageWiki i in images)
            {
                if(i != null && i.URL != null)
                {
                    string url = i.URL;
                    if(url.ToLower().EndsWith(".png") || url.ToLower().EndsWith(".gif") || url.ToLower().EndsWith(".jpeg") || url.ToLower().EndsWith(".jpg") || url.ToLower().EndsWith(".bmp"))
                    {
                        newImages.Add(i);
                    }
                }
            }
            return newImages;
        }
        public class WikipediaObject
        {
            public string title;
            public string text;
            public string imageURL;
        }

    }
}
