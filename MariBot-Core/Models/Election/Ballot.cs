namespace MariBot.Core.Models.Election
{
    public class Ballot
    {
        public string Id { get; set; } = GenerateId();
        public string VoteId => GenerateVoteId();
        public ulong ElectorId { get; set; }
        public string PollId { get; set; }
        public int Vote {  get; set; }

        public static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        public string GenerateVoteId()
        {
            return $"{PollId}:{ElectorId}";
        }

    }
}
