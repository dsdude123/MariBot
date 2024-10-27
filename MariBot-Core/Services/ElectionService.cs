using CSharpMath;
using MariBot.Core.Models.Election;
using System.Timers;

namespace MariBot.Core.Services
{
    public class ElectionService
    {
        /*
        ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠿⠛⠋⠉⡉⣉⡛⣛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣿⣿⡿⠋⠁⠄⠄⠄⠄⠄⢀⣸⣿⣿⡿⠿⡯⢙⠿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣿⡿⠄⠄⠄⠄⠄⡀⡀⠄⢀⣀⣉⣉⣉⠁⠐⣶⣶⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣿⡇⠄⠄⠄⠄⠁⣿⣿⣀⠈⠿⢟⡛⠛⣿⠛⠛⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣿⡆⠄⠄⠄⠄⠄⠈⠁⠰⣄⣴⡬⢵⣴⣿⣤⣽⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣿⡇⠄⢀⢄⡀⠄⠄⠄⠄⡉⠻⣿⡿⠁⠘⠛⡿⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⡿⠃⠄⠄⠈⠻⠄⠄⠄⠄⢘⣧⣀⠾⠿⠶⠦⢳⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⣿⣶⣤⡀⢀⡀⠄⠄⠄⠄⠄⠄⠻⢣⣶⡒⠶⢤⢾⣿⣿⣿⣿⣿⣿⣿
        ⣿⣿⣿⣿⡿⠟⠋⠄⢘⣿⣦⡀⠄⠄⠄⠄⠄⠉⠛⠻⠻⠺⣼⣿⠟⠋⠛⠿⣿⣿
        ⠋⠉⠁⠄⠄⠄⠄⠄⠄⢻⣿⣿⣶⣄⡀⠄⠄⠄⠄⢀⣤⣾⣿⣿⡀⠄⠄⠄⠄⢹
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⢻⣿⣿⣿⣷⡤⠄⠰⡆⠄⠄⠈⠉⠛⠿⢦⣀⡀⡀⠄
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠈⢿⣿⠟⡋⠄⠄⠄⢣⠄⠄⠄⠄⠄⠄⠄⠈⠹⣿⣀
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠘⣷⣿⣿⣷⠄⠄⢺⣇⠄⠄⠄⠄⠄⠄⠄⠄⠸⣿
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠹⣿⣿⡇⠄⠄⠸⣿⡄⠄⠈⠁⠄⠄⠄⠄⠄⣿
        ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⢻⣿⡇⠄⠄⠄⢹⣧⠄⠄⠄⠄⠄⠄⠄⠄⠘
         */

        private static System.Timers.Timer CheckTimer = new System.Timers.Timer { AutoReset = true, Enabled = true, Interval = 600000 };

        private DataService dataService;
        private IgdbService igdbService;

        public ElectionService(DataService dataService)
        {
            this.dataService = dataService;
        }

        private async void CheckPolls(object? sender, ElapsedEventArgs e)
        {
            var activePolls = dataService.GetPollsByStatus(PollStatus.Open);

            // Check which polls have ended
            // Calculate results
            /*
             * Create map of canidate to vote count
             * If runoff not enabled,
             * Get top caniudate group with same vote count
             * Otherwise
             * For x reamaining seats
             * Get top group of canidates with same vote count
             * Enough seats still? Fill and reduce.
             * If more canidates than seats left, do a runoff with the group and any seats filled beforehand
             */
            // Close poll
            // Perform completion callback
        }

        public string Vote(string pollId, ulong guildId, ulong channelId, ulong userId, string vote)
        {
            string resultMessage = "";

            // Get the poll
            pollId = pollId.ToLower();
            string dbId = $"{pollId}:{guildId}:{channelId}";

            Poll poll = dataService.GetPoll(pollId);
            if (poll == null)
            {
                return "Poll ID is not valid.";
            }

            if (DateTime.Now >= poll.CloseTime)
            {
                return "Poll is closed.";
            }

            // Get any previous votes
            List<Ballot> ballots = dataService.GetBallots(userId, dbId);

            // Voter eligibility check
            if (ballots.Any() && (!poll.Config.CanVoteMultipleTimes && !poll.Config.CanChangeVote))
            {
                return "User has already voted.";
                
            }

            // Perform vote validation
            Tuple<string,string> correctedVote = MatchVote(poll.Config.VoteValidationCallback, vote);
            resultMessage += correctedVote.Item2;

            int voteIndex = 0;
            bool validNumber = int.TryParse(correctedVote.Item1, out voteIndex);

            if (validNumber && voteIndex >= 0 && poll.Config.UniqueWriteInOnly)
            {
                return "Canidate already submitted.";
            }

            if (!validNumber && poll.Config.CanWriteIn)
            {
                // Perform write in and set new index
                poll.Canidates.Add(correctedVote.Item1);
                voteIndex = poll.Canidates.IndexOf(correctedVote.Item1);
                if (!dataService.UpdatePoll(poll))
                {
                    return "Failed to update poll.";
                }
            } else
            {
               return "Invalid canidate number.";
            }

            // Cast Vote
            if (ballots.Any() && poll.Config.CanChangeVote)
            {
                foreach (Ballot oldBallot in ballots)
                {
                    if (!dataService.DeleteBallot(oldBallot))
                    {
                        return "Failed to change vote.";
                    }
                }
            }
            Ballot ballot = new Ballot();
            ballot.ElectorId = userId;
            ballot.PollId = dbId;
            ballot.Vote = voteIndex;
            if (!dataService.UpdateBallot(ballot))
            {
                return "Failed to cast vote.";
            }
            resultMessage += "Your vote was cast.";
            return resultMessage;
        }

        public Tuple<string,string> MatchVote(Callback callback, string vote)
        {
            switch (callback)
            {
                case Callback.NotSet:
                    return new Tuple<string,string>(vote,"");
                case Callback.ValidateIGDB:
                    var game = igdbService.SearchGame(vote).Result;
                    if (game == null || game.Name.IsEmpty())
                    {
                        throw new ArgumentException("Couldn't match game to IGDB");
                    }
                    return new Tuple<string, string>(game.Name, $"Matched input to {game.Name} in IGDB");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
