using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public class CommandCapabilityMapping
    {
        public static Dictionary<Command, WorkerCapability> NeededCapabilityDictionary =
            new()
            {
                { Command.ConvertToDiscordFriendly, WorkerCapability.CPU }
            };
    }
}
