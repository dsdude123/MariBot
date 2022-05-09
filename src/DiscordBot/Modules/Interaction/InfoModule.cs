using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Interaction
{
    public class InfoModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ping", "Test slash command flow")]
        public async Task about()
        {
            var embed = new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "MariBot",
                Description = "Pong!"
            };

            await RespondAsync(embed: embed.Build());
        }
    }
}
