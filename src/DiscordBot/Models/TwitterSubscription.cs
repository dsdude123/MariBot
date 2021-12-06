using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models
{
    public class TwitterSubscription
    { 
        public Dictionary<ulong, HashSet<ulong>> guildChannelSubscriptions { get; set; }

        public HashSet<ulong> postedIds { get; set; }
    }
}
