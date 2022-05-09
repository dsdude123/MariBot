using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace MariBot.Modules
{
    public class RandomModule : ModuleBase<SocketCommandContext>
    {
        [Command("flipcoin")]
        public Task flipCoin()
        {
            Random result = new Random();
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
    }
}
