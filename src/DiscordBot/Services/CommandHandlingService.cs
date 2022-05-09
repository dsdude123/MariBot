using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using MariBot.Models;
using MariBot.Services;
using Newtonsoft.Json;

namespace MariBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient discord;
        private readonly CommandService commandService;
        private readonly InteractionService interactionService;
        private IServiceProvider provider;
        private StaticTextResponseService staticTextResponseService = new StaticTextResponseService();
        private FeatureToggleService featureToggleService = new FeatureToggleService();
        private PictureService pictureService = new PictureService(new System.Net.Http.HttpClient());
        private DynamicConfig dynamicConfig;

        private Regex keepOnlyAlphaNum = new Regex("[^a-zA-Z0-9 -]");

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, InteractionService interactionService)
        {
            this.discord = discord;
            commandService = commands;
            this.provider = provider;
            this.interactionService = interactionService;
        }

        public async Task InitializeAsync()
        {

            discord.MessageReceived += MessageReceived;
            discord.InteractionCreated += InteractionCreated;

            interactionService.SlashCommandExecuted += SlashCommandExecuted;
            interactionService.ContextCommandExecuted += ContextCommandExecuted;
            interactionService.ComponentCommandExecuted += ComponentCommandExecuted;

            dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config.json"));

            Timer dynamicConfigUpdate = new Timer(3300000);
            dynamicConfigUpdate.Elapsed += DynamicConfigUpdate_Elapsed;
            dynamicConfigUpdate.AutoReset = true;
            dynamicConfigUpdate.Enabled = true;

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        }

        private void DynamicConfigUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile("https://raw.githubusercontent.com/dsdude123/MariBot/master/src/DiscordBot/dynamic-config.json", "dynamic-config-temp.json");
                try
                {
                    dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config-temp.json"));
                } catch (Exception ex)
                {
                    dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config.json"));
                    return;
                }
                File.Delete("dynamic-config.json");
                File.Move("dynamic-config-temp.json", "dynamic-config.json");
            }
        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private async Task InteractionCreated(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(discord, interaction);

                if (interaction is ISlashCommandInteraction) // Check Dynamic Config if command is blocked
                {
                    ISlashCommandInteraction slashCommandInteraction = (ISlashCommandInteraction)interaction;
                    GuildConfig guildConfig = GetGuildConfig(context.Guild.Id);

                    if (guildConfig != null && guildConfig.BlockedSlashCommands != null)
                    {
                        if (guildConfig.BlockedSlashCommands.Contains(slashCommandInteraction.Data.Name.ToLower()))
                        {
                            return;
                        }
                    }
                }

                await interactionService.ExecuteCommandAsync(context, provider);
            }
            catch (Exception ex)
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(discord, message);
            GuildConfig guildConfig = GetGuildConfig(context.Guild.Id);

            // Text and File Modification Features

            if (CheckFeature(context.Guild.Id, "latex") && HasLatexMathQuotes(message.Content))
            {
                var latexTypingState = context.Channel.EnterTypingState();
                string formattedLatex = FormatLatexString(message.Content);
                var pictureService = new PictureService(new System.Net.Http.HttpClient());                
                var image = pictureService.GetLatexImage(formattedLatex).Result;
                image.Seek(0, SeekOrigin.Begin);
                await context.Channel.SendFileAsync(image, "latex.png");
                latexTypingState.Dispose();
            }

            if (CheckFeature(context.Guild.Id, "auto-jfif-to-jpeg"))
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

            if (guildConfig != null && guildConfig.AutoReactions != null)
            {
                foreach (var reactionConfig in guildConfig.AutoReactions)
                {
                    if (reactionConfig.TriggerUsers.Contains(rawMessage.Author.Id))
                    {
                        string alphaNumOnlyText = keepOnlyAlphaNum.Replace(rawMessage.Content, " ");
                        string[] words = alphaNumOnlyText.ToLower().Split(' ');
                        if (words.Intersect(reactionConfig.TriggerWords).Count() > 0)
                        {
                            var emote = Emote.Parse(reactionConfig.Emoji);
                            await rawMessage.AddReactionAsync(emote);
                        }
                    }
                }
            }


            // Text Commands

            int argPos = 0;
            string[] parts = message.Content.Split(' ');
            if (!(message.HasMentionPrefix(discord.CurrentUser, ref argPos) || message.HasStringPrefix(Program.config["prefix"] + " ",ref argPos))) return;
          
            if (parts.Length > 1)
            {
                string command = parts[1];

                if (guildConfig != null && guildConfig.BlockedTextCommands != null && guildConfig.BlockedTextCommands.Contains(command.ToLower()))
                {
                    return;
                }
            }

            var result = await commandService.ExecuteAsync(context, argPos, provider);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ToString());
                return;
            }

            // Static Text Responses

            if (result.Error.HasValue &&
                result.Error.Value == CommandError.UnknownCommand)
            {
                
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
        }

        private bool HasLatexMathQuotes(String text)
        {
            bool foundFirstQuote = false;
            int dollarCounter = 0;

            foreach(char c in text)
            {
                if(c.Equals('$'))
                {
                    dollarCounter++;
                } else if (dollarCounter > 0)
                {
                    dollarCounter--;
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

        private string FormatLatexString(String text)
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
                                if (HasLatexMathQuotes(text.Substring(nextPossibleIndex + 1)))
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

        private GuildConfig GetGuildConfig(ulong id)
        {
            foreach (var guildConfig in dynamicConfig.Guilds)
            {
                if (id.Equals(guildConfig.Id))
                {
                    return guildConfig;
                }
            }
            return null;
        }

        private bool CheckFeature(ulong id, string feature)
        {
            foreach (var guildConfig in dynamicConfig.Guilds)
            {
                if (id.Equals(guildConfig.Id) && guildConfig.EnabledFeatures != null)
                {
                    foreach (var enabledFeature in guildConfig.EnabledFeatures)
                    {
                        if (enabledFeature.Equals(feature))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;
        }
    }
}
