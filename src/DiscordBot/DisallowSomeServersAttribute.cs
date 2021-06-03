using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot
{
    public class DisallowSomeServersAttribute : PreconditionAttribute
    {
        private readonly ulong _prohibitedServer = 297485054836342786; // T

        public DisallowSomeServersAttribute()
        {
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var executingServer = context.Guild.Id;
            if (executingServer == _prohibitedServer)
            {
                Console.WriteLine("Execution of a command from a server barred from using the command was stopped.");
                return PreconditionResult.FromError("This command has been barred from use on this server.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
