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

        // TODO: Use Quartz.Net to schedule the next submission period
        private static System.Timers.Timer CheckTimer = new System.Timers.Timer { AutoReset = true, Enabled = true, Interval = 600000 };
        private static ulong VideoGameBookClubGuild = 829910467622338580;
        private static ulong VideoGameBookClubChannel = 1012515251998162974;

        private DataService dataService;
        private IgdbService igdbService;

        public ElectionService(DataService dataService)
        {
            this.dataService = dataService;
        }

        public void CreatePoll(string pollKey, ulong guildId, ulong channelId, string title, string question, List<string> canidates, DateTime closeTime, PollOptions pollOptions)
        {
            if ((pollOptions.CanChangeVote == true) && (pollOptions.CanVoteMultipleTimes == true))
            {
                throw new NotImplementedException();
            }

            Poll newPoll = new Poll
            {
                PollKey = pollKey.ToLower(),
                GuildId = guildId,
                ChannelId = channelId,
                Title = title,
                Question = question,
                Canidates = canidates,
                CloseTime = closeTime,
                Config = pollOptions,
                Status = PollStatus.Open
            };

            var existingPoll = dataService.GetPoll(newPoll.Id);

            if (existingPoll != null && existingPoll.Status != PollStatus.Closed)
            {
                throw new InvalidOperationException("A poll under this key is already open.");
            } else if (existingPoll != null)
            {
                if (!dataService.DeletePoll(existingPoll))
                {
                    throw new Exception("Failed to clean up old poll key!");
                }
                existingPoll.PollKey += Guid.NewGuid().ToString();
                dataService.UpdatePoll(existingPoll);
            }

            if (!dataService.UpdatePoll(newPoll))
            {
                throw new Exception("Failed to create poll!");
            }

            DispatchNewPollToDiscord(newPoll);
        }

        public void CreateBookClubSubmissionPoll(bool isTest = false)
        {
            string testHeader = isTest ? "[TEST] " : "";
            string testKey = isTest ? "test" : "";
            string productionNotify = isTest ? "" : " <@&1299851348761645136>";
            CreatePoll($"vgbc{testKey}", VideoGameBookClubGuild, VideoGameBookClubChannel,
                $"{testHeader}Video Game Book Club Submission Period",
                $"Submit what game you would potentially like to see in the upcoming video game book club!{productionNotify}", new List<string>(),
                DateTime.Today.AddDays(1),
                new PollOptions
                {
                    CanChangeVote = true,
                    CanVoteMultipleTimes = false,
                    CanWriteIn = true,
                    RunoffEnabled = false,
                    UniqueWriteInOnly = true,
                    VoteValidationCallback = Callback.ValidateIGDB,
                    CompletionCallback = Callback.BookClubSubmission
                });
        }


        public string Vote(string pollKey, ulong guildId, ulong channelId, ulong userId, string vote)
        {
            string resultMessage = "";

            // Get the poll
            pollKey = pollKey.ToLower();
            string dbId = $"{pollKey}:{guildId}:{channelId}";

            Poll poll = dataService.GetPoll(dbId);
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
                return "You have already voted.";
                
            }

            if (poll.Config.CanVoteMultipleTimes && (ballots.Count() > poll.Config.MultipleVoteLimit))
            {
                return "You have already used all your votes.";
            }

            // Perform vote validation
            Tuple<string,string> correctedVote = MatchVote(poll.Config.VoteValidationCallback, vote);
            resultMessage += correctedVote.Item2;

            int voteIndex = 0;
            bool validNumber = int.TryParse(correctedVote.Item1, out voteIndex);

            if (validNumber && ballots.Select(b => b.Vote == voteIndex).Any())
            {
                return "You have already voted for this canidate.";
            }

            if (!validNumber && poll.Config.CanWriteIn)
            {
                var canidateIndex = poll.Canidates.IndexOf(correctedVote.Item1);
                if (canidateIndex > -1 && poll.Config.UniqueWriteInOnly)
                {
                    return "Canidate already submitted.";
                } else if (canidateIndex > -1)
                {
                    // Found a matching canidate from write in
                    voteIndex = canidateIndex;
                } else
                {
                    // Perform write in and set new index
                    poll.Canidates.Add(correctedVote.Item1);
                    voteIndex = poll.Canidates.IndexOf(correctedVote.Item1);
                    if (!dataService.UpdatePoll(poll))
                    {
                        return "Failed to update poll.";
                    }
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

        private void CompletePoll(Poll poll)
        {
            switch (poll.Config.CompletionCallback)
            {
                case Callback.NotSet:
                    if (poll.Config.RunoffEnabled)
                    {
                        var winingCanidates = DetermineRunoffCanidates(poll.Results, poll.Config.Seats);
                        if (winingCanidates.Count > poll.Config.Seats)
                        {
                            // Runoff needed
                            List<string> runoffCanidates = new List<string>();
                            foreach (var index in winingCanidates)
                            {
                                runoffCanidates.Add(poll.Canidates[index]);
                            }
                            CreatePoll(poll.PollKey, poll.GuildId, poll.ChannelId,
                                $"[RUNOFF] {poll.Title}", poll.Question, runoffCanidates, DateTime.Today.AddDays(1),
                                poll.Config);
                        } else
                        {
                            DispatchResultsToDiscord(poll);
                        }
                    } else
                    {
                        DispatchResultsToDiscord(poll);
                    }
                    break;
                case Callback.BookClubSubmission:
                    bool isTest = false;
                    if (poll.PollKey.Equals("vgbctest"))
                    {
                        isTest = true;
                    }
                    string testHeader = isTest ? "[TEST] " : "";
                    string testKey = isTest ? "test" : "";
                    string productionNotify = isTest ? "" : " <@&1299851348761645136>";

                    CreatePoll($"vgbc{testKey}",
                        poll.GuildId, poll.ChannelId,
                        $"{testHeader}Video Game Book Club Final Vote",
                        $"Vote for what video game you want to see in the next book club! Top two canidates win.{productionNotify}",
                        poll.Canidates, DateTime.Today.AddDays(1),
                        new PollOptions
                        {
                            CanChangeVote = false,
                            CanVoteMultipleTimes = false,
                            CanWriteIn = false,
                            RunoffEnabled = true,
                            Seats = 2,
                            VoteValidationCallback = Callback.NotSet,
                            CompletionCallback = Callback.NotSet
                        });
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async void CheckPolls(object? sender, ElapsedEventArgs e)
        {
            var activePolls = dataService.GetPollsByStatus(PollStatus.Open);

            foreach (var poll in activePolls)
            {
                var votes = dataService.GetBallots(poll.Id)
                    .Select(ballot => ballot.Vote)
                    .GroupBy(vote => vote)
                    .Select(group => new Result { Canidate = group.Key, Votes = group.Count() })
                    .OrderByDescending(top => top.Votes);

                poll.Results = votes;
                poll.Status = PollStatus.Closed;
                dataService.UpdatePoll(poll);
                CompletePoll(poll);
            }
        }

        private List<int> DetermineRunoffCanidates(IOrderedEnumerable<Result> results, int seats)
        {
            // Inverse and regroup the results to get list of canidates for a set number of votes
            // i.e. Group 1 is Canidate A with 12 votes. Group 2 is Canidates B and C with 5 votes each, a tie

            List<int> winningCanidates = new List<int>();

            var canidateGroups = results.GroupBy(result => result.Votes)
                .Select(seat => new { Votes = seat.Key, Canidates = seat.ToList() })
                .OrderByDescending(seat => seat.Votes);

            var groupEnumerator = canidateGroups.GetEnumerator();

            while (seats > 0 && groupEnumerator.MoveNext())
            {
                winningCanidates.AddRange((IEnumerable<int>)groupEnumerator.Current.Canidates);
                seats -= groupEnumerator.Current.Canidates.Count;
            }

            return winningCanidates;
        }

        private void DispatchNewPollToDiscord(Poll poll)
        {
            // TODO: Finish me
        }

        private void DispatchResultsToDiscord(Poll poll)
        {
            // TODO: Finish me
        }
    }
}
