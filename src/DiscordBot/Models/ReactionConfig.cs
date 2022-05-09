using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models
{
    public class ReactionConfig
    {
        public string[] TriggerWords { get; set; }
        public ulong[] TriggerUsers { get; set; }
        public string Emoji { get; set; }
    }
}
