using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Models
{
    public class GameStore
    {
        public Dictionary<ulong, UserProfile> profiles;
    }

    public class UserProfile
    {
        public DateTime lastDailyCredit = DateTime.MinValue;
        public ulong credits = 0;
    }
}
