using Discord.Commands;

namespace MariBot.Core.Modules.Text
{
    public class RandomModule : ModuleBase<SocketCommandContext>
    {
        [Command("flipcoin")]
        public Task flipCoin()
        {
            Random result = new Random(GuidToRandomSeed(Guid.NewGuid()));
            int rint = result.Next(0, 2);
            if (rint == 0)
            {
                return ReplyAsync("The coin landed on heads.\n");
            }
            else
            {
                return ReplyAsync("The coin landed on tails.\n");
            }
        }

        private static int GuidToRandomSeed(Guid guid)
        {
            int seed = 0;
            foreach (var character in guid.ToByteArray())
            {
                seed += character;
            }
            return seed;
        }
    }
}
