using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanDictionaryDex.Client;
using UrbanDictionaryDex.Models;

namespace MariBot.Services
{
    public class UrbanDictionaryService
    {
        public async Task<DefinitionData> GetRandomWord()
        {
            var client = new UrbanDictionaryClient();
            return client.GetRandomTerm().Result.First();
        }

        public async Task<DefinitionData> GetTopDefinition(string word)
        {
            var client = new UrbanDictionaryClient();
            var results = await client.SearchTerm(word.ToLower());
            bool foundOne = false;
            DefinitionData topDefinition = new DefinitionData();
            foreach (var item in results)
            {
                if (item.Word.ToLower().Equals(word.ToLower())) {
                    foundOne = true;
                    var topScore = topDefinition.ThumbsUp - topDefinition.ThumbsDown;
                    var newScore = item.ThumbsUp - item.ThumbsDown;

                    if (newScore > topScore)
                    {
                        topDefinition = item;
                    }
                }
            }

            if (foundOne)
            {
                return topDefinition;
            } else
            {
                throw new ArgumentException("No result found");
            }
        }
    }
}
