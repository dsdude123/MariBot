using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Interaction
{
    public class InfoModule : InteractionModuleBase<IInteractionContext>
    {
        [SlashCommand("ping", "Test slash command flow")]
        public async Task about()
        {
            var embed = new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "SpookyMari",
                Description = "Pong!"
            };

            await RespondAsync(embed: embed.Build());
        }
    }
}
