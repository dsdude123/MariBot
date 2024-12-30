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

        [Command("voteresults", RunMode = RunMode.Async)]
        public async Task VoteResults(string pollKey)
        {
            var poll = electionService.CheckResults(pollKey, Context.Guild.Id, Context.Channel.Id);
            if (poll == null)
            {
                await Context.Channel.SendMessageAsync("Poll not found.", messageReference: new MessageReference(Context.Message.Id));
                return;
            }
            var eb = new EmbedBuilder();
            eb.WithTitle(poll.Title);

            string description = "Here are the current results for the poll:\n\n";

            if (poll.Results != null && poll.Results.Count > 0)
            {
                foreach (var canidate in poll.Results)
                {
                    description += $"{canidate.Votes} votes - {poll.Canidates[canidate.Canidate]}\n";
                }
            } else
            {
                description += "No results.\n";
            }

            description += $"\nThe poll is currently {poll.Status}\n";
            description += $"The poll close time is {poll.CloseTime.ToString("f")} Pacific.";

            eb.WithDescription(description);           

            await Context.Channel.SendMessageAsync(embed: eb.Build(), messageReference: new MessageReference(Context.Message.Id));
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
