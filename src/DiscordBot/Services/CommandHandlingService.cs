using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using MariBot.Services;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private StaticTextResponseService staticTextResponseService = new StaticTextResponseService();
        private FeatureToggleService featureToggleService = new FeatureToggleService();
        private PictureService pictureService = new PictureService(new System.Net.Http.HttpClient());

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_discord, message);

            if (hasLatexMathQuotes(message.Content))
            {
                var latexTypingState = context.Channel.EnterTypingState();
                string formattedLatex = formatLatexString(message.Content);
                var pictureService = new PictureService(new System.Net.Http.HttpClient());                
                var image = pictureService.GetLatexImage(formattedLatex).Result;
                image.Seek(0, SeekOrigin.Begin);
                await context.Channel.SendFileAsync(image, "latex.png");
                latexTypingState.Dispose();
            }

            if (featureToggleService.CheckFeature("auto-jfif-to-jpeg", context.Guild.Id.ToString()))
            {
                foreach (Attachment attachment in context.Message.Attachments)
                {
                    if (attachment.Filename.EndsWith(".jfif") || attachment.Filename.EndsWith(".JFIF")
                        || attachment.Filename.EndsWith(".jfif-large"))
                    {
                        pictureService.ConvertJfifToJpeg(context, attachment.Url);
                    }
                }
            }

            int argPos = 0;
            if (!(message.HasMentionPrefix(_discord.CurrentUser, ref argPos) || message.HasStringPrefix(Program._config["prefix"] + " ",ref argPos))) return;

            
            //var typingState = context.Channel.EnterTypingState();
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ToString());
                return;
            }

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
            {
                string[] parts = message.Content.Split(' ');
                if (parts.Length >= 2) {
                    string textResponseLookup = parts[1];
                    Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
                    if(userIdCheck.IsMatch(textResponseLookup))
                    {
                        textResponseLookup = textResponseLookup.Replace("!", string.Empty);
                    }

                    string response = StaticTextResponseService.getGlobalResponse(textResponseLookup) 
                        ?? staticTextResponseService.getResponse(context.Guild.Id, textResponseLookup);

                    if(response != null)
                    await context.Channel.SendMessageAsync(response);
                }
            }

            //typingState.Dispose();
        }

        private bool hasLatexMathQuotes(String text)
        {
            bool foundFirstQuote = false;
            int dollarCounter = 0;

            foreach(char c in text)
            {
                if(c.Equals('$'))
                {
                    dollarCounter++;
                }

                if(dollarCounter == 2)
                {
                    if(!foundFirstQuote)
                    {
                        foundFirstQuote = true;
                        dollarCounter = 0;
                    } else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private string formatLatexString(String text)
        {
            string formattedString = "";
            bool inMathBlock = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c.Equals('$'))
                {
                    int nextPossibleIndex = i + 1;
                    if (inMathBlock)
                    {
                        if(nextPossibleIndex < text.Length)
                        {
                            char nextChar = text[nextPossibleIndex];
                            if(nextChar.Equals('$')) // end of math block
                            {
                                formattedString += "$$\\text{";
                                inMathBlock = false;
                                i++;
                                continue;
                            } else
                            {
                                formattedString += c;
                                continue;
                            }
                        } else
                        {
                            formattedString += c;
                        } 
                    } else
                    {
                        // check if we can enter the math block
                        if (nextPossibleIndex < text.Length)
                        {
                            char nextChar = text[nextPossibleIndex];
                            if (nextChar.Equals('$'))
                            {
                                // looks like start of a block, make sure it's balanced
                                if (hasLatexMathQuotes(text.Substring(nextPossibleIndex + 1)))
                                {
                                    // positive for math block
                                    if (formattedString.Equals("")){
                                        formattedString += "$$";
                                    } else
                                    {
                                        formattedString += "}$$";
                                    }
                                    inMathBlock = true;
                                    i++;
                                    continue;
                                }
                            }
                        } 
                    }
                } else
                {
                    if(formattedString.Equals(""))
                    {
                        formattedString += "\\text{";
                    }
                    formattedString += c;
                }
            }

            if (!inMathBlock)
            {
                formattedString += "}";
            }
            return formattedString;
        }
    }
}
