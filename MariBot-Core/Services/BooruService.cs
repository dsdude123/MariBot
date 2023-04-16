using BooruSharp.Search.Post;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class BooruService
    {

        /*
         * Not supported by this service but supported by the library: 
         * Atfbooru 
         * E621 
         * E926 
         * Furry Booru 
         * Lolibooru
         */

        // NOTE: Safebooru and Sakugabooru seem to be safe, add check to validate

        public SearchResult getRandomDanbooruDonmai(string[] tags)
        {
            var booru = new BooruSharp.Booru.DanbooruDonmai();
            return booru.GetRandomPostAsync(tags).Result;
        }
        public SearchResult getRandomGelbooru(string[] tags)
        {
            var booru = new BooruSharp.Booru.Gelbooru();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomKonachan(string[] tags)
        {
            var booru = new BooruSharp.Booru.Konachan();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomRealbooru(string[] tags)
        {
            var booru = new BooruSharp.Booru.Realbooru();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomRule34(string[] tags)
        {
            var booru = new BooruSharp.Booru.Rule34();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomSafebooru(string[] tags)
        {
            var booru = new BooruSharp.Booru.Safebooru();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomSakugabooru(string[] tags)
        {
            var booru = new BooruSharp.Booru.Sakugabooru();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomSankakuComplex(string[] tags)
        {
            var booru = new BooruSharp.Booru.SankakuComplex();
            return booru.GetRandomPostAsync(tags).Result;
        }

        public SearchResult getRandomXbooru(string[] tags)
        {
            var booru = new BooruSharp.Booru.Xbooru();
            return booru.GetRandomPostAsync(tags).Result;
        }
        
        public SearchResult getRandomYandere(string[] tags)
        {
            var booru = new BooruSharp.Booru.Yandere();
            return booru.GetRandomPostAsync(tags).Result;
        }
    }
}
