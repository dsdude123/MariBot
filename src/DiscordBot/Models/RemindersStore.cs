using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MariBot.Models
{
    public class RemindersStore
    {
        public List<ReminderObject> reminders;
    }

    public class ReminderObject
    {
        public ulong server;
        public ulong channel;
        public string message;
        public DateTime alarmTime;
    }
}
