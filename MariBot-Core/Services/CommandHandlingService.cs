using System.Globalization;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using CSharpMath.Rendering.FrontEnd;
using CSharpMath.SkiaSharp;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Modules.Text;
using SkiaSharp;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    public class CommandHandlingService
    {
        private static readonly string[] twitterDomains = new string[] { "twitter.com", "x.com", "www.twitter.com", "www.x.com" };
        private static readonly Regex keepOnlyAlphaNum = new("[^a-zA-Z0-9 -]");
        private static readonly Regex latexDetector = new("\\$[^$]+\\$");

        private readonly IConfiguration configuration;
        private readonly CommandService commandService;
        private readonly DataService dataService;
        private readonly DiscordSocketClient discord;
        private readonly DynamicConfigService dynamicConfigService;
        private readonly ImageService imageService;
        private readonly InteractionService interactionService;
        private readonly OpenAiService openAiService;
        private readonly ILogger<CommandHandlingService> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly StaticTextResponseService staticTextResponseService;
        private readonly WorkerManagerService workerManagerService;

        private Dictionary<string, int> failedEmbedRemoves = new Dictionary<string, int>();
        private System.Timers.Timer twitterCleanup = new Timer { AutoReset = true, Enabled = false, Interval = 5000 };

        public CommandHandlingService(DataService dataService, DynamicConfigService dynamicConfigService, ILogger<CommandHandlingService> logger, IServiceProvider serviceProvider, DiscordSocketClient discord, CommandService commandService, InteractionService interactionService, IConfiguration configuration, StaticTextResponseService staticTextResponseService, ImageService imageService, OpenAiService openAiService, WorkerManagerService workerManagerService)
        {
            this.dataService = dataService;
            this.dynamicConfigService = dynamicConfigService;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.discord = discord;
            this.commandService = commandService;
            this.interactionService = interactionService;
            this.configuration = configuration;
            this.staticTextResponseService = staticTextResponseService;
            this.imageService = imageService;
            this.openAiService = openAiService;
            this.workerManagerService = workerManagerService;

            twitterCleanup.Elapsed += HandleTimer;
        }

        public async Task InitializeAsync()
        {
            logger.LogInformation("Starting command handler");
            discord.MessageReceived += MessageReceived;
            discord.InteractionCreated += InteractionCreated;

            interactionService.SlashCommandExecuted += SlashCommandExecuted;
            interactionService.ContextCommandExecuted += ContextCommandExecuted;
            interactionService.ComponentCommandExecuted += ComponentCommandExecuted;

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            // TODO: Implement Result handling
            return Task.CompletedTask;
        }

        private async Task InteractionCreated(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(discord, interaction);

                if (interaction is ISlashCommandInteraction slashCommandInteraction) // Check Dynamic Config if command is blocked
                {
                    var requestedCommand = slashCommandInteraction.Data.Name.ToLower();
                    logger.LogInformation("User \"{}\" requested command \"{}\" in \"{}/{}\"", context.User.ToString(), requestedCommand, context.Guild.ToString(), context.Channel.ToString());

                    if (dynamicConfigService.IsSlashCommandBlocked(context.Guild.Id, requestedCommand))
                    {
                        logger.LogWarning("An attempt was made to execute a blocked command.");
                        return;
                    }
                }

                await interactionService.ExecuteCommandAsync(context, serviceProvider);
            }
            catch (Exception ex)
            {
                if (interaction.Type == InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            try
            {
                // Ignore system messages and messages from bots
                if (!(rawMessage is SocketUserMessage message)) return;
                if (message.Source != MessageSource.User) return;
                var context = new SocketCommandContext(discord, message);
                // Send to data service
                dataService.WriteDiscordMessage(context);

                // Handle Emoji Auto Reactions
                await HandleEmojiAutoReactions(context);

                //TODO: Add hooks for file modifications

                var argPos = 0;
                var parts = message.Content.Split(' ');

                // Check if message has prefix to trigger the bot
                var prefix = configuration.GetValue<string>("DiscordSettings:Prefix");
                if ((message.HasMentionPrefix(discord.CurrentUser, ref argPos) || message.HasStringPrefix(prefix + " ", ref argPos)))
                {
                    using var typingState = context.Channel.EnterTypingState();

                    if (parts.Length < 2) return; // Make sure we actually have a command
                    var requestedCommand = parts[1];
                    logger.LogInformation("User \"{}\" requested command \"{}\" in \"{}/{}\"", context.User.ToString(), requestedCommand, context.Guild.ToString(), context.Channel.ToString());

                    // Check if we are allowed to run this command in this guild. Stop if we aren't.
                    if (dynamicConfigService.IsTextCommandBlocked(context.Guild.Id, requestedCommand))
                    {
                        logger.LogWarning("An attempt was made to execute a blocked command.");
                        return;
                    }

                    var staticResponse = staticTextResponseService.GetResponse(requestedCommand, context.Guild.Id);
                    if (staticResponse != null)
                    {
                        logger.LogInformation("Found matching static text response for {}", requestedCommand);
                        await context.Channel.SendMessageAsync(staticResponse);
                    }
                    else
                    {
                        var result = await commandService.ExecuteAsync(context, argPos, serviceProvider);

                        if (result.Error.HasValue &&
                            result.Error.Value != CommandError.UnknownCommand)
                        {
                            logger.LogError("Command encountered an error. {}", result.ToString());
                            await context.Channel.SendMessageAsync(result.ToString());
                        }
                    }
                } else if (message.Type == MessageType.Reply)
                {
                    openAiService.HandleReply(context);
                }
                else
                {
                    // Anti-Elon Feature
                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "auto-vxtwitter"))
                    {
                        var vxParts = context.Message.Content.Split(" ");

                        foreach (var text in vxParts)
                        {
                            try
                            {
                                var url = new Uri(text);

                                if (twitterDomains.Contains(url.Host, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    var urlBuilder = new UriBuilder(url);
                                    urlBuilder.Host = "vxtwitter.com";
                                    await context.Channel.SendMessageAsync(urlBuilder.ToString());

                                    try
                                    {
                                        await context.Message.ModifyAsync(message =>
                                        {
                                            message.Flags = MessageFlags.SuppressEmbeds;
                                        });

                                        var modified = await context.Channel.GetMessageAsync(context.Message.Id);
                                        if (modified.Embeds.Count > 0)
                                        {
                                            throw new Exception("Failed to remove embed");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogWarning(
                                            "Couldn't remove Twitter embed, are permissions or the embed missing?");
                                        failedEmbedRemoves.Add(
                                            $"{context.Guild.Id}-{context.Channel.Id}-{context.Message.Id}", 0);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "auto-ddinstagram"))
                    {
                            var ddParts = context.Message.Content.Split(" ");

                            foreach (var text in ddParts)
                            {
                                try
                                {
                                    var url = new Uri(text);

                                    if (url.Host.EndsWith("instagram.com", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        var urlBuilder = new UriBuilder(url);
                                        urlBuilder.Host = "ddinstagram.com";
                                        await context.Channel.SendMessageAsync(urlBuilder.ToString());

                                        try
                                        {
                                            await context.Message.ModifyAsync(message =>
                                            {
                                                message.Flags = MessageFlags.SuppressEmbeds;
                                            });

                                            var modified = await context.Channel.GetMessageAsync(context.Message.Id);
                                            if (modified.Embeds.Count > 0)
                                            {
                                                throw new Exception("Failed to remove embed");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogWarning("Couldn't remove Instagram embed, are permissions or the embed missing?");
                                            failedEmbedRemoves.Add($"{context.Guild.Id}-{context.Channel.Id}-{context.Message.Id}", 0);
                                        }
                                    }
                                }
                                catch { }
                            }
                    }

                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "auto-rxddit"))
                    {
                        var ddParts = context.Message.Content.Split(" ");

                        foreach (var text in ddParts)
                        {
                            try
                            {
                                var url = new Uri(text);

                                if (url.Host.EndsWith("reddit.com", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    var urlBuilder = new UriBuilder(url);
                                    urlBuilder.Host = "rxddit.com";
                                    await context.Channel.SendMessageAsync(urlBuilder.ToString());

                                    try
                                    {
                                        await context.Message.ModifyAsync(message =>
                                        {
                                            message.Flags = MessageFlags.SuppressEmbeds;
                                        });

                                        var modified = await context.Channel.GetMessageAsync(context.Message.Id);
                                        if (modified.Embeds.Count > 0)
                                        {
                                            throw new Exception("Failed to remove embed");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogWarning("Couldn't remove Reddit embed, are permissions or the embed missing?");
                                        failedEmbedRemoves.Add($"{context.Guild.Id}-{context.Channel.Id}-{context.Message.Id}", 0);
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "auto-vxtiktok"))
                    {
                        var ddParts = context.Message.Content.Split(" ");

                        foreach (var text in ddParts)
                        {
                            try
                            {
                                var url = new Uri(text);

                                if (url.Host.EndsWith("tiktok.com", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    var urlBuilder = new UriBuilder(url);
                                    urlBuilder.Host = "vxtiktok.com";
                                    await context.Channel.SendMessageAsync(urlBuilder.ToString());

                                    try
                                    {
                                        await context.Message.ModifyAsync(message =>
                                        {
                                            message.Flags = MessageFlags.SuppressEmbeds;
                                        });

                                        var modified = await context.Channel.GetMessageAsync(context.Message.Id);
                                        if (modified.Embeds.Count > 0)
                                        {
                                            throw new Exception("Failed to remove embed");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogWarning("Couldn't remove Reddit embed, are permissions or the embed missing?");
                                        failedEmbedRemoves.Add($"{context.Guild.Id}-{context.Channel.Id}-{context.Message.Id}", 0);
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "auto-image-conversion"))
                    {
                        if (message.Attachments.Count > 0)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                if (attachment.Filename.EndsWith(".avif", true, CultureInfo.InvariantCulture) ||
                                    attachment.Filename.EndsWith(".jfif", true, CultureInfo.InvariantCulture))
                                {
                                    logger.LogInformation("Queueing automatic image conversion");
                                    var image = imageService.GetWebResource(attachment.Url).Result as MemoryStream;
                                    image.Seek(0, SeekOrigin.Begin);

                                    var conversionJob = new WorkerJob();
                                    conversionJob.Id = Guid.NewGuid();
                                    conversionJob.GuildId = context.Guild.Id;
                                    conversionJob.ChannelId = context.Channel.Id;
                                    conversionJob.MessageId = context.Message.Id;
                                    conversionJob.SourceImage = image.ToArray();
                                    conversionJob.Command = Command.ConvertToDiscordFriendly;

                                    workerManagerService.EnqueueJob(conversionJob);
                                }
                            }
                        }
                    }

                    // LaTeX handling
                    if (dynamicConfigService.CheckFeatureEnabled(context.Guild.Id, "latex"))
                    {
                        if (latexDetector.IsMatch(context.Message.Content))
                        {
                            logger.LogInformation("Detected possible LaTeX message, rendering...");
                            var latexImage = imageService.GenerateLatexImage(context.Message.Content);
                            await context.Channel.SendFileAsync(latexImage, "latex.png", messageReference: new MessageReference(context.Message.Id));
                        }
                    }
                }
            } catch (Exception ex)
            {
                logger.LogCritical("Unhandled exception in command handler. {}", ex.Message);
            }

        }

        /// <summary>
        /// Checks message content against the auto emoji reaction config and automatically adds an emoji reaction if a match is found.
        /// </summary>
        /// <param name="context">Discord message context</param>
        /// <returns>Task</returns>
        private async Task HandleEmojiAutoReactions(SocketCommandContext context)
        {
            var autoReactions = dynamicConfigService.GetReactionConfig(context.Guild.Id);

            foreach (var reactionConfig in autoReactions)
            {
                if (reactionConfig.TriggerUsers.Contains(context.User.Id))
                {
                    var alphaNumOnlyText = keepOnlyAlphaNum.Replace(context.Message.Content, " ");
                    var words = alphaNumOnlyText.ToLower().Split(' ');
                    if (words.Intersect(reactionConfig.TriggerWords).Any())
                    {
                        logger.LogInformation("Auto reacting to message.");
                        var emote = Emote.Parse(reactionConfig.Emoji);
                        await context.Message.AddReactionAsync(emote);
                    }
                }
            }
        }

        private async void HandleTimer(object source, ElapsedEventArgs arg)
        {
            List<string> toRemove = new List<string>();
            foreach (var message in failedEmbedRemoves)
            {
                if (message.Value >= 3)
                {
                    toRemove.Add(message.Key);
                }
                else
                {
                    string[] parts = message.Key.Split('-');
                    try
                    {
                        IGuild guild = FindServer(Convert.ToUInt64(parts[0]));
                        ITextChannel channel = FindTextChannel(guild, Convert.ToUInt64(parts[1]));
                        IMessage discordMessage = channel.GetMessageAsync(Convert.ToUInt64(parts[2])).Result;

                        if (discordMessage.Embeds.Count > 0)
                        {
                            await channel.ModifyMessageAsync(discordMessage.Id, properties =>
                            {
                                properties.Flags = MessageFlags.SuppressEmbeds;
                            });

                            var modified = await channel.GetMessageAsync(Convert.ToUInt64(parts[2]));
                            if (modified.Embeds.Count > 0)
                            {
                                throw new Exception("Failed to remove embed");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        failedEmbedRemoves[message.Key]++;
                    }
                }
            }

            foreach (var message in toRemove)
            {
                failedEmbedRemoves.Remove(message);
            }
        }

        private IGuild FindServer(ulong id)
        {
            foreach (IGuild server in discord.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        private ITextChannel FindTextChannel(IGuild server, ulong id)
        {
            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }
    }
}
