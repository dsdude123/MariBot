using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using StarBot.Services;
using UrbanDictionnet;
using static StarBot.Services.WikipediaService;

namespace StarBot.Modules
{
    /*
     * Collection of commands for looking up things like definitions.
     */
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        public UrbanDictionaryService UrbanDictionaryService { get; set; }
        public WikipediaService WikipediaService { get; set; }

        [Command("urban")]
        public Task urban([Remainder] string word)
        {
            DefinitionData result;
            try
            {
                result = UrbanDictionaryService.GetRandomDefinition(word).Result;
            }
            catch (AggregateException ex)
            {
                return ReplyAsync(ex.InnerException.Message);
            }
            string output ="";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb);
        }

        [Command("urbanrand")]
        public Task urbanrand()
        {
            var result = UrbanDictionaryService.GetRandomWord().Result;
            string output = "";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb);
        }

        [Command("urbantop")]
        public Task urbantop([Remainder] string word)
        {
            DefinitionData result;
            try
            {
                result = UrbanDictionaryService.GetTopDefinition(word).Result;
            }
            catch (AggregateException ex)
            {
                return ReplyAsync(ex.InnerException.Message);
            }

            string output = "";
            result.Definition = result.Definition.Replace("[", "");
            result.Definition = result.Definition.Replace("]", "");
            result.Example = result.Example.Replace("[", "");
            result.Example = result.Example.Replace("]", "");
            output += "**" + result.Word + "'s definition**\n\n";
            output += result.Definition + "\n\n";
            output += "**Example**\n\n";
            output += result.Example + "\n\n";
            output += "**Upvotes** " + result.ThumbsUp + " **Downvotes** " + result.ThumbsDown;
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Green;
            return Context.Channel.SendMessageAsync("", false, eb);
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

            string output = "";
            output += "**" + result.title + "**\n\n";
            output += result.text;

            output = trimToLength(output, 2048);

            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Blue;

            if (result.imageURL != null)
            {
                eb.ImageUrl = result.imageURL;
            }

            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb);
        }

        [Command("wikisearch")]
        public Task wikipediaSearch([Remainder] string topic)
        {
            var state = Context.Channel.EnterTypingState();
            var result = WikipediaService.GetWikipediaResults(topic).Result;
            string output = "";
            output += "**Results for " + topic + " **\n\n";
            for (int i = 0; i < result.Count; i++)
            {
                output += result[i] + "\n";
            }
            var eb = new EmbedBuilder();
            eb.WithDescription(output);
            eb.Color = Color.Blue;
            state.Dispose();
            return Context.Channel.SendMessageAsync("", false, eb);
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
    }
}
