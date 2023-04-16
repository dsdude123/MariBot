using Discord;
using Discord.Commands;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Services;
using RestSharp.Extensions;

namespace MariBot.Core.Modules.Text
{
    public class ImageModule : ModuleBase<SocketCommandContext>
    {
        private ImageService imageService { get; set; }
        private WorkerManagerService workerManagerService { get; set; }
        private OpenAiService openAiService { get; set; }

        public ImageModule(ImageService imageService, WorkerManagerService workerManagerService, OpenAiService openAiService)
        {
            this.imageService = imageService;
            this.workerManagerService = workerManagerService;
            this.openAiService = openAiService;
        }

        [Command("sonicsays", RunMode = RunMode.Async)]
        public async Task sonicsays([Remainder] string text)
        {
            HandleCommonTextScenario(Command.SonicSays, text);
        }

        [Command("9gag", RunMode = RunMode.Async)]
        public async Task NineGag([Remainder] string text)
        {
            HandleCommonImageScenario(Command.NineGag);
        }

        [Command("adidas", RunMode = RunMode.Async)]
        public async Task Adidas([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Adidas);
        }

        [Command("adw", RunMode = RunMode.Async)]
        public async Task AdminWalk([Remainder] string text)
        {
            HandleCommonImageScenario(Command.AdminWalk);
        }

        [Command("aew", RunMode = RunMode.Async)]
        public async Task AEW([Remainder] string text)
        {
            HandleCommonImageScenario(Command.AEW);
        }

        [Command("ai-image", RunMode = RunMode.Async)]
        public async Task aiimage([Remainder] string prompt)
        {
            HandleCommonTextScenario(Command.StableDiffusion, prompt);
        }

        [Command("ai-pokemon", RunMode = RunMode.Async)]
        public async Task aipokemon([Remainder] string prompt)
        {
            HandleCommonTextScenario(Command.StableDiffusionPokemon, prompt);
        }

        [Command("ai-waifu", RunMode = RunMode.Async)]
        public async Task aiwaifu([Remainder] string prompt)
        {
            HandleCommonTextScenario(Command.StableDiffusionWaifu, prompt);
        }

        [Command("ajit", RunMode = RunMode.Async)]
        public async Task Ajit([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Ajit);
        }

        [Command("america", RunMode = RunMode.Async)]
        public async Task America([Remainder] string text)
        {
            HandleCommonImageScenario(Command.America);
        }

        [Command("analysis", RunMode = RunMode.Async)]
        public async Task Analysis([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Analysis);
        }

        [Command("andrew", RunMode = RunMode.Async)]
        public async Task Andrew([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Andrew);
        }

        [Command("asuka", RunMode = RunMode.Async)]
        public async Task Asuka([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Asuka);
        }

        [Command("austin", RunMode = RunMode.Async)]
        public async Task Austin([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Austin);
        }

        [Command("banner", RunMode = RunMode.Async)]
        public async Task HangTheBanner([Remainder] string text)
        {
            HandleCommonTextScenario(Command.Banner, text);
        }

        [Command("bernie", RunMode = RunMode.Async)]
        public async Task Bernie([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Bernie);
        }

        [Command("biden", RunMode = RunMode.Async)]
        public async Task Biden([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Biden);
        }

        [Command("binoculars", RunMode = RunMode.Async)]
        public async Task Binoculars([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Binoculars);
        }

        [Command("bobross", RunMode = RunMode.Async)]
        public async Task BobRoss([Remainder] string text)
        {
            HandleCommonImageScenario(Command.BobRoss);
        }

        [Command("cmm", RunMode = RunMode.Async)]
        public async Task ChangeMyMind([Remainder] string text)
        {
            HandleCommonTextScenario(Command.ChangeMyMind, text);
        }

        [Command("condom", RunMode = RunMode.Async)]
        public async Task Condom([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Condom);
        }

        [Command("daryl", RunMode = RunMode.Async)]
        public async Task Daryl([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Daryl);
        }

        [Command("dalle", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task dalle([Remainder] string prompt)
        {
            try
            {
                string imageUrl = openAiService.ExecuteDalleQuery(prompt).Result;
                await Context.Channel.SendMessageAsync($"{imageUrl}", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ArgumentException)
            {
                await Context.Channel.SendMessageAsync("Your input prompt failed safety checks.", messageReference: new MessageReference(Context.Message.Id));
            }
            catch (ApplicationException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.Message}", messageReference: new MessageReference(Context.Message.Id));
            }
        }

        [Command("dave", RunMode = RunMode.Async)]
        public async Task dave([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Dave);
        }

        [Command("deepfry", RunMode = RunMode.Async)]
        public async Task Deepfry([Remainder] string text)
        {
            HandleCommonImageScenario(Command.DeepFry);
        }

        [Command("dkoldies", RunMode = RunMode.Async)]
        public async Task DKOldies([Remainder] string text)
        {
            HandleCommonImageScenario(Command.DkOldies);
        }

        [Command("dskoopa", RunMode = RunMode.Async)]
        public async Task Dskoopa([Remainder] string text)
        {
            HandleCommonImageScenario(Command.DsKoopa);
        }

        [Command("nuke", RunMode = RunMode.Async)]
        public async Task Nuke([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Nuke);
        }

        [Command("e2h", RunMode = RunMode.Async)]
        public async Task Edges2Hentai([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Edges2Hentai);
        }

        [Command("herschel", RunMode = RunMode.Async)]
        public async Task Herschel([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Herschel);
        }

        [Command("kevin", RunMode = RunMode.Async)]
        public async Task kevin([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Kevin);
        }

        [Command("kurisu", RunMode = RunMode.Async)]
        public async Task Kurisu([Remainder] string text)
        {
            HandleCommonTextScenario(Command.Kurisu, text);
        }

        [Command("makoto", RunMode = RunMode.Async)]
        public async Task Makoto([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Makoto);
        }

        [Command("miyamoto", RunMode = RunMode.Async)]
        public async Task Miyamoto([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Miyamoto);
        }

        [Command("obama", RunMode = RunMode.Async)]
        public async Task Obama([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Obama);
        }

        [Command("pence", RunMode = RunMode.Async)]
        public async Task Pence([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Pence);
        }

        [Command("queen", RunMode = RunMode.Async)]
        public async Task Queen([Remainder] string text)
        {
            HandleCommonTextScenario(Command.Queen, text);
        }

        [Command("radical", RunMode = RunMode.Async)]
        public async Task RadicalReggie([Remainder] string text)
        {
            HandleCommonImageScenario(Command.RadicalReggie);
        }

        [Command("reagan", RunMode = RunMode.Async)]
        public async Task Reagan([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Reagan);
        }

        [Command("rgt", RunMode = RunMode.Async)]
        public async Task RGT([Remainder] string text)
        {
            HandleCommonImageScenario(Command.RGT);
        }

        [Command("scarecrow", RunMode = RunMode.Async)]
        public async Task Scarecrow([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Scarecrow);
        }

        [Command("spawnwave", RunMode = RunMode.Async)]
        public async Task Spawnwave([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Spawnwave);
        }

        [Command("trump", RunMode = RunMode.Async)]
        public async Task Trump([Remainder] string text)
        {
            HandleCommonImageScenario(Command.Trump);
        }

        private async void HandleCommonImageScenario(Command command)
        {
            var imageUrl = await imageService.GetImageUrl(Context);
            var image = await imageService.GetWebResource(imageUrl);

            var job = BuildImageJob(Context, command, image);
            await Context.Channel.SendMessageAsync(workerManagerService.EnqueueJob(job),
                messageReference: new MessageReference(Context.Message.Id));
        }

        private async void HandleCommonTextScenario(Command command, string text)
        {
            var job = BuildTextJob(Context, command, text);
            await Context.Channel.SendMessageAsync(workerManagerService.EnqueueJob(job),
                messageReference: new MessageReference(Context.Message.Id));
        }

        private WorkerJob BuildImageJob(SocketCommandContext context, Command command, Stream image)
        {
            return new WorkerJob()
            {
                ChannelId = context.Channel.Id,
                GuildId = context.Guild.Id,
                MessageId = context.Message.Id,
                Command = command,
                Id = Guid.NewGuid(),
                SourceImage = image.ReadAsBytes()
            };
        }

        private WorkerJob BuildTextJob(SocketCommandContext context, Command command, string prompt)
        {
            return new WorkerJob()
            {
                ChannelId = context.Channel.Id,
                GuildId = context.Guild.Id,
                MessageId = context.Message.Id,
                Command = command,
                Id = Guid.NewGuid(),
                SourceText = prompt
            };
        }
    }
}
