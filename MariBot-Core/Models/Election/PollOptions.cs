namespace MariBot.Core.Models.Election
{
    public class PollOptions
    {
        public bool CanVoteMultipleTimes { get; set; }
        public int MultipleVoteLimit { get; set; } 
        public bool CanChangeVote {  get; set; }
        public bool RunoffEnabled {  get; set; }
        public int Seats { get; set; }
        public bool CanWriteIn {  get; set; }
        public bool UniqueWriteInOnly { get; set; }
        public Callback VoteValidationCallback { get; set; }
        public Callback CompletionCallback { get; set; }
    }
}
