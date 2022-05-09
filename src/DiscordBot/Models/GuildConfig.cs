using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models
{
    public class GuildConfig
    {
        public ulong Id { get; set; }
        public string Name { get; set; } // Informational only
        public string[] EnabledFeatures { get; set; }
        public string[] BlockedTextCommands { get; set; }
        public string[] BlockedSlashCommands { get; set; }
        public ReactionConfig[] AutoReactions { get; set; }
    }
}
