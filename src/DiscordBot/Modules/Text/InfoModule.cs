using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using ImageMagick;
using MariBot.Models;
using MariBot.Modules;
using MariBot.Services;
using Newtonsoft.Json;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace MariBot.Modules
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        ///     The Biohazard symbol emoji.
        /// </summary>
        private readonly static string BIOHAZARD = "☣️";
        private readonly static Emoji BiohazardEmoji = new Emoji(BIOHAZARD);

        public PictureService PictureService { get; set; }

        public TalkHubService TalkHubService { get; set; } // TODO: Move this into its own module

        public OpenAIService OpenAiService { get; set; }

        public InfoModule(DiscordSocketClient discordClient)
        {
            discordClient.ReactionAdded += ReactionAddedHandler;
        }

        [Command("help")]
        public Task help()
        {
            if (Context.Guild.Id == 297485054836342786) // Server is prohibited from using some commands
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/297485054836342786/commands.html");
            }
            else if (Context.Guild.Id == 564645677586710548)
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/564645677586710548/commands.html");
            }
            else
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/commands.html");
            }
        }

        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net\n"); // TODO: Fix version

        [Command("tts", RunMode = RunMode.Async)]
        public async Task tts([Remainder]string text)
        {
            try
            {
                CreateTTS(text).WaitForExit();
            } catch (FileNotFoundException e)
            {
                await Context.Channel.SendMessageAsync("SharpTalkGenerator is not installed.");
                return;
            }
            var channel = (Context.Guild.GetUser(Context.Message.Author.Id))?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendFileAsync(Environment.CurrentDirectory + "\\tts.wav");
                return;
            }
            var audioClient = await channel.ConnectAsync();
            await SendAsync(audioClient, Environment.CurrentDirectory + "\\tts.wav");
            audioClient.StopAsync();
        }

        [Command("yoda", RunMode = RunMode.Async)]
        public async Task yoda([Remainder]string text)
        {
            TalkHubService.GetTextToSpeech(Context, "yoda", text);
        }

        [Command("radar")]
        public async Task radar(string stateCode = "WA")
        {
            string url;

            switch (stateCode.ToUpper())
            {
                case "WA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/tiw/animate.png";
                    break;
                case "NC": 
                case "SC":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/clt/animate.png";
                    break;
                case "VA":
                case "MD":
                case "DC":
                case "WV":
                case "NJ":
                case "DE":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/shd/animate.png";
                    break;
                case "OR":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/rdm/animate.png";
                    break;
                case "CO":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/den/animate.png";
                    break;
                case "CA":
                case "CA-SOUTH":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bfl/animate.png";
                    break;
                case "NY":
                case "PA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bgm/animate.png";
                    break;
                case "ND":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bis/animate.png";
                    break;
                case "NH":
                case "VT":
                case "ME":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bml/animate.png";
                    break;
                case "TX":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/sat/animate.png"; // This one is problematic because Texas is too big
                    break;
                case "KY":
                case "TN":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/bwg/animate.png";
                    break;
                case "MI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/cad/animate.png";
                    break;
                case "GA":
                case "AL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/csg/animate.png";
                    break;
                case "OH":
                case "IN":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/day/animate.png";
                    break;
                case "IA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/dsm/animate.png";
                    break;
                case "CT":
                case "MA":
                case "RI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/hfd/animate.png";
                    break;
                case "MO":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/jef/animate.png";
                    break;
                case "OK":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/law/animate.png";
                    break;
                case "NE":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lbf/animate.png";
                    break;
                case "AR":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lit/animate.png";
                    break;
                case "MT":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/lwt/animate.png";
                    break;
                case "LA":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/msy/animate.png";
                    break;
                case "ID":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/myl/animate.png";
                    break;
                case "FL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pie/animate.png";
                    break;
                case "SD":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pir/animate.png";
                    break;
                case "AZ":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/prc/animate.png";
                    break;
                case "UT":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/pvu/animate.png";
                    break;
                case "WY":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/riw/animate.png";
                    break;
                case "NV":
                case "CA-NORTH":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/rno/animate.png";
                    break;
                case "NM":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/row/animate.png";
                    break;
                case "KS":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/sln/animate.png";
                    break;
                case "IL":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/spi/animate.png";
                    break;
                case "MN":
                case "WI":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/stc/animate.png";
                    break;
                case "MS":
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/county_loc/tvr/animate.png";
                    break;
                case "US":
                default:
                    url = "https://s.w-x.co/staticmaps/wu/wxtype/none/usa/animate.png";
                    break;
            }
            if (stateCode.ToUpper().Equals("AK") || stateCode.ToUpper().Equals("HI"))
            {
                Context.Channel.SendMessageAsync("Sorry there is no radar map avalible, defaulting to contiguous United States");
            }
            var image = PictureService.GetPictureAsync(url).Result;
            image.Seek(0, SeekOrigin.Begin);
            MemoryStream outgoingImage = new MemoryStream();

            using (var baseImage = new MagickImageCollection(image))
            {
                using (var outputCollection = new MagickImageCollection())
                {
                    bool usedBaseOnce = false;
                    baseImage.Coalesce();
                    foreach(var frame in baseImage)
                    {
                        outputCollection.Add(new MagickImage(frame));
                    }
                    outputCollection.Write(outgoingImage, MagickFormat.Gif);
                } 
            }
            outgoingImage.Seek(0, SeekOrigin.Begin);
            Context.Channel.SendFileAsync(outgoingImage, "radar.gif");
        }

        [Command("latex")]
        public async Task latex([Remainder] string latex)
        {
            var image = PictureService.GetLatexImage(latex).Result;
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "latex.png");
        }

        [Command("solve")]
        public Task solve([Remainder] string equation)
        {
            org.mariuszgromada.math.mxparser.Expression expression = new org.mariuszgromada.math.mxparser.Expression(equation);
            return Context.Channel.SendMessageAsync(expression.calculate().ToString());
        }

        [Command("waifu", RunMode = RunMode.Async)]
        public async Task waifu()
        {
            string id = Guid.NewGuid().ToString();
            string filePath = Environment.CurrentDirectory + $"\\cache\\{Context.Guild.Id}\\{id}.png";
            Directory.CreateDirectory(Environment.CurrentDirectory + $"\\cache\\{Context.Guild.Id}");

            Process waifulabs = CreateWaifu(filePath);
            waifulabs.WaitForExit();
            if(File.Exists(filePath))
            {
                await Context.Channel.SendFileAsync(filePath);
            } else
            {
                await Context.Channel.SendMessageAsync("Failed to generate waifu.");
            }
        }

        [Command("uwu")]
        public async Task UwuifyAsync([Remainder] string input = null)
        {
            if(input == null)
            {
                input = (await Context.Channel.GetMessagesAsync(2, Discord.CacheMode.AllowDownload)
                    .FlattenAsync())
                    .ToArray()
                    .OrderBy(message => message.Timestamp)
                    .First().Content;
            }
            input = UwuifyText(input);
            
            await Context.Channel.SendMessageAsync(input);
        }

        [Command("gpt3", RunMode = RunMode.Async)]
        public async Task OpenAiTextCompletion([Remainder] string input)
        {
            var moderationResult = await OpenAiService.CreateModeration(new CreateModerationRequest()
            {
                Input = input,
                Model = "text-moderation-latest"
            });

            foreach (var moderation in moderationResult.Results)
            {
                if (moderation.Flagged)
                {
                    await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
                    return;
                }
            }

            var textResult = await OpenAiService.CreateCompletion(new CompletionCreateRequest()
            {
                Prompt = input,
                MaxTokens = 500
            }, OpenAI.GPT3.ObjectModels.Models.TextDavinciV3);

            if (textResult.Successful)
            {
                var text = textResult.Choices.FirstOrDefault().Text;

                if (text.Length > 1992)
                {
                    text = text.Substring(0, 1992);
                }
                await Context.Channel.SendMessageAsync($"```\n{text}\n```", messageReference: new MessageReference(Context.Message.Id));
            } else
            {
                await Context.Channel.SendMessageAsync($"{textResult.Error.Code}: {textResult.Error.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        private string UwuifyText(string input)
        {
            input = Regex.Replace(input, "(?:r|l)", "w");
            input = Regex.Replace(input, "(?:R|L)", "W");
            input = Regex.Replace(input, "n([aeiou])", "ny$1");
            input = Regex.Replace(input, "N([aeiou])", "Ny$1");
            input = Regex.Replace(input, "N([AEIOU])", "NY$1");
            input = Regex.Replace(input, "ove", "uv");
            return input;
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Process CreateTTS(string text)
        {
            if(!File.Exists("SharpTalkGenerator.exe"))
            {
                throw new FileNotFoundException("SharpTalk is not installed.");
            }
            return Process.Start(new ProcessStartInfo
            {
                FileName = "SharpTalkGenerator",
                Arguments = $"{text}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Process CreateWaifu(string path)
        {
            if (!File.Exists("waifulabs.exe"))
            {
                throw new FileNotFoundException("WaifuLabs.NET is not installed.");
            }
            return Process.Start(new ProcessStartInfo
            {
                FileName = "waifulabs",
                Arguments = $"{path}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> userMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            Task.Run(async () =>
            {
                if (reaction.Emote.Name == BIOHAZARD)
                {
                    var message = await userMessage.GetOrDownloadAsync();
                    var channelContext = await channel.GetOrDownloadAsync();
                    SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channelContext;
                    if (CheckEmojiTriggerFeature(socketGuildChannel.Guild.Id) && message.Reactions.TryGetValue(BiohazardEmoji, out var reactionMetadata) && !reactionMetadata.IsMe)
                    {
                        var text = UwuifyText(message.Content);
                        await channelContext.SendMessageAsync(text);
                        await message.AddReactionAsync(BiohazardEmoji);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private bool CheckEmojiTriggerFeature(ulong id)
        {
            DynamicConfig dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                        System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config.json"));
            foreach (var guildConfig in dynamicConfig.Guilds)
            {
                if (guildConfig.Id.Equals(id) && guildConfig.EnabledFeatures != null)
                {
                    return Array.Exists(guildConfig.EnabledFeatures, x => x.Equals("emoji-triggers"));
                }
            }
            return false;
        }
    }
}
