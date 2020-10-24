using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UrbanDictionnet;

namespace MariBot.Services
{
    public class UrbanDictionaryService
    {
        private readonly UrbanClient UrbanDictionary;

        public UrbanDictionaryService(UrbanClient uc)
            => UrbanDictionary = uc;

        public async Task<UrbanDictionnet.DefinitionData> GetRandomDefinition(string word)
        {
            WordDefine result = UrbanDictionary.GetWordAsync(word).Result;
            if (result.ResultType == ResultType.NoResults || result.List.Count < 1)
            {
                throw new WordNotFoundException("No results were found.");
            }

            if (result.List.Count > 1)
            {
                Random r = new Random();
                int toChoose = r.Next(0, result.List.Count);
                return result.List[toChoose];
            }
            else
            {
                return result.List[0];
            }

        }

        public async Task<UrbanDictionnet.DefinitionData> GetRandomWord()
        {
            return UrbanDictionary.GetRandomWordAsync().Result;
        }

        public async Task<UrbanDictionnet.DefinitionData> GetTopDefinition(string word)
        {
            WordDefine result = UrbanDictionary.GetWordAsync(word).Result;
            if (result.ResultType == ResultType.NoResults || result.List.Count < 1)
            {
                throw new WordNotFoundException("No results were found.");
            }

            if (result.List.Count > 1)
            {
                int bestScore = int.MinValue;
                int bestIndex = -1;
                for (int i = 0; i < result.List.Count; i++)
                {
                    DefinitionData j = result.List[i];
                    if ((j.ThumbsUp - j.ThumbsDown) > bestScore)
                    {
                        bestScore = j.ThumbsUp - j.ThumbsDown;
                        bestIndex = i;
                    }
                }

                return result.List[bestIndex];
            }
            else
            {
                return result.List[0];
            }

        }
    }
}
