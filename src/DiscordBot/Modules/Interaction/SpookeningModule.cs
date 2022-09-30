using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Interaction
{
    public class SpookeningModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("doot", "Test slash command flow")]
        public async Task doot()
        {

        }
    }
}
