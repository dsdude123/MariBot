using CSharpMath;
using Discord.WebSocket;
using LinqToTwitter;
using MariBot.Core.Models.Election;
using RestSharp.Validation;
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

        private static System.Timers.Timer CheckTimer = new System.Timers.Timer { AutoReset = true, Enabled = true, Interval = 60000 };
        private static ulong VideoGameBookClubGuild = 829910467622338580;
        private static ulong VideoGameBookClubChannel = 1012515251998162974;
        private static ulong TestVideoGameBookClubGuild = 829910467622338580;
        private static ulong TestVideoGameBookClubChannel = 1033157890623676426;
        private static DateTime NextSubmissionPeriod;

        private readonly DataService dataService;
        private readonly IgdbService igdbService;
        private readonly DiscordSocketClient discord;
        private readonly ILogger<ElectionService> logger;

        public ElectionService(DataService dataService, IgdbService igdbService, DiscordSocketClient discord, ILogger<ElectionService> logger)
        {
            this.dataService = dataService;
            this.igdbService = igdbService;
            this.discord = discord;
            this.logger = logger;
            CheckTimer.Elapsed += CheckPolls;
            NextSubmissionPeriod = GetNextVideoGameBookClub();
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

            logger.LogInformation($"New poll ID is: {newPoll.Id}");

            var existingPoll = dataService.GetPoll(newPoll.Id);

            if (existingPoll != null && existingPoll.Status != PollStatus.Closed && !pollKey.ToLower().Equals("vgbctest"))
            {
                logger.LogWarning("A poll under this key is already open.");
                return;
            } else if (existingPoll != null)
            {
                logger.LogInformation("Cleaning up old poll");
                if (!dataService.DeletePoll(existingPoll))
                {
                    logger.LogCritical("Failed to delete old poll");
                    return;
                }
                existingPoll.PollKey += Guid.NewGuid().ToString();
                dataService.UpdatePoll(existingPoll);

                var oldBallots = dataService.GetBallots(newPoll.Id);
                foreach ( var ballot in oldBallots )
                {
                    logger.LogInformation($"Cleaning up ballot {ballot.Id}");
                    if (!dataService.DeleteBallot(ballot))
                    {
                        logger.LogCritical("Failed to migrate old vote during cleanup phase.");
                        return;
                    }

                    ballot.PollId = existingPoll.Id;

                    if (!dataService.UpdateBallot(ballot))
                    {
                        logger.LogCritical("Failed to migrate old vote during republish phase.");
                        return;
                    }

                }
            }

            if (!dataService.UpdatePoll(newPoll))
            {
                logger.LogError("Failed to create poll!");
                return;
            }

            DispatchNewPollToDiscord(newPoll);
        }

        public void CreateBookClubSubmissionPoll(bool isTest = false)
        {
            string testHeader = isTest ? "[TEST] " : "";
            string testKey = isTest ? "test" : "";
            string productionNotify = isTest ? "" : " <@&1299851348761645136>";
            CreatePoll($"vgbc{testKey}", 
                isTest ? TestVideoGameBookClubGuild : VideoGameBookClubGuild, 
                isTest? TestVideoGameBookClubChannel : VideoGameBookClubChannel,
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

            logger.LogInformation($"Incoming vote for poll {dbId}. Vote {vote}. Elector {userId}");

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
            } else if (!validNumber)
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

        public Poll CheckResults(string pollKey, ulong guildId, ulong channelId)
        {
            // Get the poll
            pollKey = pollKey.ToLower();
            string dbId = $"{pollKey}:{guildId}:{channelId}";
            logger.LogInformation($"Getting poll results for {dbId}");

            Poll poll = dataService.GetPoll(dbId);
            if (poll == null)
            {
                return null;
            }

            var votes = dataService.GetBallots(poll.Id)
                .Select(ballot => ballot.Vote)
                .GroupBy(vote => vote)
                .Select(group => new Result { Canidate = group.Key, Votes = group.Count() })
                .OrderByDescending(top => top.Votes)
                .ToList();

            poll.Results = votes;
            return poll;
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
                    return new Tuple<string, string>(game.Name, $"Matched input to {game.Name} in IGDB.\n");
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

                    if (poll.Canidates.Count() < 1)
                    {
                        return;
                    }

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
            if (DateTime.Now >= NextSubmissionPeriod)
            {
                CreateBookClubSubmissionPoll();
                NextSubmissionPeriod = GetNextVideoGameBookClub();
            }
            var activePolls = dataService.GetPollsByStatus(PollStatus.Open);

            foreach (var poll in activePolls)
            {
                if (DateTime.Now < poll.CloseTime)
                {
                    continue;
                }
                var votes = dataService.GetBallots(poll.Id)
                    .Select(ballot => ballot.Vote)
                    .GroupBy(vote => vote)
                    .Select(group => new Result { Canidate = group.Key, Votes = group.Count() })
                    .OrderByDescending(top => top.Votes)
                    .ToList();

                poll.Results = votes;
                poll.Status = PollStatus.Closed;
                dataService.UpdatePoll(poll);
                CompletePoll(poll);
            }
        }

        private List<int> DetermineRunoffCanidates(List<Result> results, int seats)
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
                foreach (Result result in  groupEnumerator.Current.Canidates)
                {
                    winningCanidates.Add(result.Canidate);
                }
                seats -= groupEnumerator.Current.Canidates.Count;
            }

            return winningCanidates;
        }

        private void DispatchNewPollToDiscord(Poll poll)
        {
            var eb = new Discord.EmbedBuilder();
            eb.WithTitle(poll.Title);

            string description = "";
            description += poll.Question;
            description += "\n\n";
            if (poll.Canidates.Any())
            {
                description += "Canidates:\n";
                for (int i = 0; i < poll.Canidates.Count; i++)
                {
                    description += $"{i} - {poll.Canidates[i]}\n";
                }
                description += "\n\n";
            }

            if (poll.Config.CanVoteMultipleTimes)
            {
                description += $"Multiple votes allowed, limit {poll.Config.MultipleVoteLimit}.\n";
            } 

            if (poll.Config.CanChangeVote)
            {
                description += "Vote change allowed.\n";
            }

            if (poll.Config.CanWriteIn)
            {
                description += "Write-in votes accepted.\n";
            }

            description += $"Voting closes at {poll.CloseTime.ToString("f")} Pacific.\n\n";
            description += $"Vote using command: `z vote {poll.PollKey} <choice>`\nCheck poll results using command: `z voteresults {poll.PollKey}`";

            eb.WithDescription(description);

            var guild = FindServer(poll.GuildId);
            var destination = FindTextChannel(guild, poll.ChannelId);

            destination.SendMessageAsync(embed: eb.Build());
        }

        private void DispatchResultsToDiscord(Poll poll)
        {
            var eb = new Discord.EmbedBuilder();
            eb.WithTitle($"[POLL RESULTS] {poll.Title}");

            string description = "";

            foreach(Result result in poll.Results)
            {
                description += $"{result.Votes} votes - {poll.Canidates[result.Canidate]}\n";
            }

            eb.WithDescription(description);

            var guild = FindServer(poll.GuildId);
            var destination = FindTextChannel(guild, poll.ChannelId);

            destination.SendMessageAsync(embed: eb.Build());
        }

        private Discord.IGuild FindServer(ulong id)
        {
            foreach (Discord.IGuild server in discord.Guilds)
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        private Discord.ITextChannel FindTextChannel(Discord.IGuild server, ulong id)
        {
            foreach (Discord.ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }

        private DateTime GetNextVideoGameBookClub()
        {
            DateTime now = DateTime.Now;
            DateTime endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            DateTime sevenDaysBeforeEndOfMonth = endOfMonth.AddDays(-7);

            if (now >= sevenDaysBeforeEndOfMonth) {
                now = now.AddDays(14);
                endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                var nextMonth = endOfMonth.AddDays(-7);
                logger.LogInformation($"Next Video Game Book Club Submission Period is {nextMonth.ToString("f")}");
                return nextMonth;
            } else
            {
                logger.LogInformation($"Next Video Game Book Club Submission Period is {sevenDaysBeforeEndOfMonth.ToString("f")}");
                return sevenDaysBeforeEndOfMonth;
            }
        }
    }
}
