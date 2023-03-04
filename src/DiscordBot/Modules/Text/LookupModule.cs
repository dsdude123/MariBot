using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Responses;
using Google.Apis.Customsearch.v1.Data;
using Google.Apis.Kgsearch.v1.Data;
using MariBot.Models.Google.KnowledgeGraph;
using MariBot.Services;
using Newtonsoft.Json;
using UrbanDictionaryDex.Models;
using static MariBot.Services.WikipediaService;

namespace MariBot.Modules
{
    /*
     * Collection of commands for looking up things like definitions.
     */
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        public GoogleService GoogleService { get; set; }

        public PictureService PictureService { get; set; }
        public UrbanDictionaryService UrbanDictionaryService { get; set; }
        public WikipediaService WikipediaService { get; set; }

        public WolframAlphaClient WolframAlphaService = new WolframAlphaClient(MariBot.Program.config["wolframAlphaAppId"]);

        [Command("urban")]
        public Task urban([Remainder] string word)
        {
            DefinitionData result;
            try
            {
                result = UrbanDictionaryService.GetTopDefinition(word).Result;
            }
            catch (Exception)
            {
                var errorEmbed = new EmbedBuilder();
                errorEmbed.WithTitle("Urban Dictionary");
                errorEmbed.WithDescription("Definition does not exist or an unknown error occurred.");
                errorEmbed.Color = Color.Red;
                return Context.Channel.SendMessageAsync("", false, errorEmbed.Build());
            }
            string output = "**" + result.Word + "'s definition**\n\n";
            output += result.Definition.Replace("[","").Replace("]","") + "\n\n";
            output += "**Example**\n\n";
            output += result.Example.Replace("[", "").Replace("]", "") + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithTitle("Urban Dictionary");
            eb.WithUrl(result.Permalink);
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("urbanrand")]
        public Task urbanrand()
        {
            var result = UrbanDictionaryService.GetRandomWord().Result;
            string output = "**" + result.Word + "'s definition**\n\n";
            output += result.Definition.Replace("[", "").Replace("]", "") + "\n\n";
            output += "**Example**\n\n";
            output += result.Example.Replace("[", "").Replace("]", "") + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithTitle("Urban Dictionary");
            eb.WithUrl(result.Permalink);
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("wiki")]
        public Task wikipedia([Remainder] string topic)
        {
            var state = Context.Channel.EnterTypingState();
            WikipediaObject result;

            try
            {
                result = WikipediaService.GetWikipediaPage(topic).Result;
            } catch (Exception e)
            {
                state.Dispose();
                return Context.Channel.SendMessageAsync(e.Message);
            }

            string output = result.text;
            output = trimToLength(output, 2048);

            var eb = new EmbedBuilder();
            eb.WithTitle(result.title);
            eb.WithDescription(output);
            eb.WithColor(Color.Blue);
            eb.WithUrl(result.link);

            if (result.imageURL != null)
            {
                eb.WithImageUrl(result.imageURL);
            }

            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("wikisearch")]
        public Task wikipediaSearch([Remainder] string topic)
        {
            var state = Context.Channel.EnterTypingState();
            var result = WikipediaService.GetWikipediaResults(topic).Result;
            string output = "";
            for (int i = 0; i < result.Count; i++)
            {
                output += result[i] + "\n";
            }
            var eb = new EmbedBuilder();
            eb.WithTitle("Results for " + topic);
            eb.WithDescription(output);
            eb.WithColor(Color.Blue);
            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("google", RunMode = RunMode.Async)]
        public async Task google([Remainder] string keywords)
        {
            Search searchResults = GoogleService.Search(keywords).Result;
            var eb = new EmbedBuilder();
            if (searchResults.Items.Count < 1)
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Red);
                eb.WithDescription("No results were found.");
            } else
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Green);
                string description = "";
                foreach(Result result in searchResults.Items)
                {
                    string formattedResult = ConvertSearchToMessage(result);
                    if((description + formattedResult).Length <= 2048)
                    {
                        description += formattedResult;
                    }
                }
                eb.WithDescription(description);
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("kg", RunMode = RunMode.Async)]
        public async Task knowledgegraph([Remainder] string keywords)
        {
            SearchResponse searchResponse = GoogleService.KnowledgeGraph(keywords).Result;
            var eb = new EmbedBuilder();
            if (searchResponse.ItemListElement.Count < 1)
            {
                eb.WithTitle("Google Knowledge Graph");
                eb.WithColor(Color.Red);
                eb.WithDescription("No result was found.");
            }
            else
            {
                eb.WithAuthor("Google Knowledge Graph");
                eb.WithColor(Color.Green);

                EntitySearchResult result = JsonConvert.DeserializeObject<EntitySearchResult>(searchResponse.ItemListElement[0].ToString());
                eb.WithTitle(WebUtility.HtmlDecode(result.Result.Name));
                eb.WithDescription(ConvertKnowledgeGraphEntityToMessage(result.Result));

                if (result.Result.DetailedDescription != null)
                {
                    eb.WithUrl(result.Result.DetailedDescription.Url);
                } else
                {
                    eb.WithUrl(result.Result.Url);
                }

                if(result.Result.Image != null)
                eb.WithThumbnailUrl(result.Result.Image.ContentUrl);
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("image", RunMode = RunMode.Async)]
        public async Task googleimage([Remainder] string keywords)
        {
            Search searchResults = GoogleService.Search(keywords, true).Result;
            var eb = new EmbedBuilder();
            string imageUrl = GetFirstValidImageUrl(searchResults);
            if (imageUrl != null)
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Green);
                eb.WithImageUrl(imageUrl);
            }
            else
            {
                eb.WithTitle("Google Search");
                eb.WithColor(Color.Red);
                eb.WithDescription("No results were found.");
            }
            await Context.Channel.SendMessageAsync(embed: eb.Build());
        }

        [Command("wa", RunMode = RunMode.Async)]
        public async Task wolframAlphaSimple([Remainder] string query)
        {
            FullResultResponse result = await WolframAlphaService.FullResultAsync(query);
            Queue<string> messageQueue = new Queue<string>();
            if(result.IsSuccess)
            {
                string outputBuffer = "";
                foreach (var pod in result.Pods)
                {
                    string title = (pod.Title.Length > 0) ? ("**" + pod.Title + "**\n") : ""; 
                    string newBuffer = outputBuffer + title;
                    if (newBuffer.Length < 2000)
                    {
                        outputBuffer = newBuffer;
                    }
                    else
                    {
                        messageQueue.Enqueue(outputBuffer);
                        outputBuffer = title;
                    }
                    foreach (var subPod in pod.SubPods)
                    {
                        title = (subPod.Title.Length > 0) ? ("**" + subPod.Title + "**\n") : "";

                        if (subPod.Plaintext.Length > 0)
                        {
                            newBuffer = outputBuffer + title + subPod.Plaintext + "\n";

                            if (newBuffer.Length < 2000)
                            {
                                outputBuffer = newBuffer;
                            }
                            else
                            {
                                messageQueue.Enqueue(outputBuffer);
                                outputBuffer = title + subPod.Plaintext + "\n";
                            }
                        } else
                        {
                            newBuffer = outputBuffer + title;

                            if (newBuffer.Length < 2000)
                            {
                                messageQueue.Enqueue(newBuffer);
                                outputBuffer = "";
                            }
                            else
                            {
                                messageQueue.Enqueue(outputBuffer);
                                outputBuffer = title + subPod.Plaintext + "\n";
                                messageQueue.Enqueue(outputBuffer);
                                outputBuffer = "";
                            }
                            messageQueue.Enqueue(subPod.Image.Src.ToString());
                        }

                    }
                }
                if(outputBuffer.Length > 0)
                {
                    messageQueue.Enqueue(outputBuffer);
                }
                if(messageQueue.Count > 0)
                {
                    while(messageQueue.Count > 0)
                    {
                        string message = messageQueue.Dequeue();
                        if (message.Length > 0)
                        {
                            await Context.Channel.SendMessageAsync(message);
                        }
                        Thread.Sleep(1000);
                    }
                }
            } else
            {
                Context.Channel.SendMessageAsync("Sorry there was an issue talking to Wolfram Alpha.");
            }
        }

        private String trimToLength(String text, int maxLength)
        {
            if(maxLength < 0)
            {
                throw new ArgumentException("Invalid argument maxLength");
            }

            if(maxLength > text.Length)
            {
                return text;
            }

            int totalToRemove = text.Length - maxLength;

            return text.Remove(maxLength, totalToRemove);
        }

        private string ConvertSearchToMessage(Result result)
        {
            return "**" + result.Title + "**\n" + result.Link + "\n";
        }

        private string ConvertKnowledgeGraphEntityToMessage(Entity entity)
        {
            if (entity.DetailedDescription != null)
            {
                return WebUtility.HtmlDecode("*" + entity.Description + "*\n\n" + entity.DetailedDescription.ArticleBody);
            } else
            {
                return WebUtility.HtmlDecode("*" + entity.Description + "*");
            }
        }

        private string GetFirstValidImageUrl(Search searchResult)
        {
            foreach(Result result in searchResult.Items) // Try to find the full image first
            {
                try
                {
                    if (IsContentTypeImage(result.Link))
                    {
                        return result.Link;
                    }
                    else if (IsContentTypeImage(result.Image.ContextLink))
                    {
                        return result.Image.ContextLink;
                    }
                } catch (Exception ex)
                {
                    // Do nothing this is fine
                }
            }

            foreach (Result result in searchResult.Items) // Somehow didn't find an image, get the first thumbnail
            {
                try
                {
                    if (IsContentTypeImage(result.Image.ThumbnailLink))
                    {
                        return result.Link;
                    }
                } catch (Exception ex)
                {
                    // Do nothing this is fine
                }
            }
            return null; // Something is probably wrong if we get here.
        }

        private bool IsContentTypeImage(String url)
        {
            try
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";
                httpRequest.Method = "HEAD";
                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                return httpResponse.Headers.Get("Content-Type").StartsWith("image/") || httpResponse.Headers.Get("content-type").StartsWith("image/");
            } catch (Exception ex)
            {
                return false;
            }
        }
    }
}
