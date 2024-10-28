using Discord;
using Discord.Commands;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    public class ElectionModule : ModuleBase<SocketCommandContext>
    {
        private ElectionService electionService { get; set; }

        public ElectionModule(ElectionService electionService)
        {
            this.electionService = electionService;
        }

        [Command("vote", RunMode = RunMode.Async)]
        public async Task Vote(string pollKey, [Remainder] string vote)
        {
            string result = electionService.Vote(pollKey, Context.Guild.Id, Context.Channel.Id, Context.User.Id, vote);
            await Context.Channel.SendMessageAsync($"{result}", messageReference: new MessageReference(Context.Message.Id));
        }

        [Command("vgbctest", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task VideoGameBookClubTest()
        {
            electionService.CreateBookClubSubmissionPoll(isTest: true);
        }

        [Command("vgbc", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task VideoGameBookClubForce()
        {
            electionService.CreateBookClubSubmissionPoll(isTest: false);
        }
    }
}
