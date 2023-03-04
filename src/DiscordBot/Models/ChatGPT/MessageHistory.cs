using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.ChatGPT
{
    public class MessageHistory
    {
        public MessageHistory()
        {
            Messages = new List<Tuple<MessageType, string>>();
        }

        public List<Tuple<MessageType, string>> Messages { get; set; }
    }
}
