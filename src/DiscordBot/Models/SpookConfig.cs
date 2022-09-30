using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models
{
    public class SpookConfig
    {
        public ulong TargetGuild { get; set; }
        public ulong MessageChannelId { get; set; }
        public ulong SpookyRoleId { get; set; }
        public uint SpookUserLimit { get; set; }
        public List<ulong> OverrideUserIds { get; set; }
        public List<string> SpookyEmojis { get; set; }
        public List<string> NicknameFormatters { get; set; }
        public List<string> SpookyJokes { get; set; }
    }
}
