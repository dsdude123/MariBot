using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.ChatGPT
{
    public class MessageStore
    {
        public Dictionary<string, MessageHistory> sessions { get; set; }

        public MessageStore()
        {
            sessions = new Dictionary<string, MessageHistory>();
        }

        public bool KeyExists(ulong guildId, ulong channelId, ulong messageId)
        {
            return sessions.ContainsKey(GenerateId(guildId, channelId, messageId));
        }

        public MessageHistory DropMessageHistory(ulong guildId, ulong channelId, ulong messageId)
        {
            string id = GenerateId(guildId, channelId, messageId);
            try
            {
                MessageHistory result = sessions[id];
                sessions.Remove(id);
                return result;
            }
            catch (Exception)
            {
                return new MessageHistory();
            }
        }

        public void PutMessageHistory(ulong guildId, ulong channelId, ulong messageId, MessageHistory messageHistory)
        {
            sessions.Add(GenerateId(guildId, channelId, messageId), messageHistory);
        }

        public void UpdateId(ulong guildId, ulong channelId, ulong messageId, ulong newMessageId)
        {
            var oldId = GenerateId(guildId, channelId, messageId);
            var newId = GenerateId(guildId, channelId, newMessageId);
            sessions.Add(newId, sessions[oldId]);
            sessions.Remove(oldId);
        }

        private string GenerateId(ulong guildId, ulong channelId, ulong messageId)
        {
            return $"{guildId}-{channelId}-{messageId}";
        }
    }
}
