using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.Discord
{
    public class Message
    {
        public string Id => $"{GuildId}:{ChannelId}:{MessageId}";
        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public ulong AuthorId { get; set; }
        public MessageSource MessageSource { get; set; }
        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? EditedTimestamp { get; set; }
        public ulong? MessageReference { get; set; }
        public MessageType Type { get; set; }
        public List<Attachment> Attachments { get; set; }

        public static Message FromSocketMessage(SocketMessage socketMessage, ulong guildId = 0)
        {
            var message = new Message
            {
                MessageId = socketMessage.Id,
                ChannelId = socketMessage.Channel.Id,
                GuildId = guildId,
                AuthorId = socketMessage.Author.Id,
                MessageSource = (MessageSource)socketMessage.Source,
                Content = socketMessage.Content,
                CreatedAt = socketMessage.CreatedAt,
                EditedTimestamp = socketMessage.EditedTimestamp
            };
            if (socketMessage.Reference != null) message.MessageReference = socketMessage.Reference.MessageId.GetValueOrDefault(0);
            message.Type = (MessageType)socketMessage.Type;

            message.Attachments = new List<Attachment>();
            foreach(var attachment in socketMessage.Attachments)
            {
                var convertedAttachment = new Attachment
                {
                    Id = attachment.Id,
                    Filename = attachment.Filename,
                    Url = attachment.Url,
                    ProxyUrl = attachment.ProxyUrl,
                    Size = attachment.Size,
                    Height = attachment.Height,
                    Width = attachment.Width,
                    Ephemeral = attachment.Ephemeral,
                    Description = attachment.Description,
                    ContentType = attachment.ContentType
                };
                message.Attachments.Add(convertedAttachment);
            }

            return message;
        }

        public static Message FromSocketCommandContext(SocketCommandContext context)
        {
            var socketMessage = context.Message;

            var message = new Message
            {
                MessageId = socketMessage.Id,
                ChannelId = context.Channel.Id,
                GuildId = context.Guild.Id,
                AuthorId = socketMessage.Author.Id,
                MessageSource = (MessageSource)socketMessage.Source,
                Content = socketMessage.Content,
                CreatedAt = socketMessage.CreatedAt,
                EditedTimestamp = socketMessage.EditedTimestamp
            };
            if (socketMessage.Reference != null) message.MessageReference = socketMessage.Reference.MessageId.GetValueOrDefault(0);
            message.Type = (MessageType)socketMessage.Type;

            message.Attachments = new List<Attachment>();
            foreach (var attachment in socketMessage.Attachments)
            {
                var convertedAttachment = new Attachment
                {
                    Id = attachment.Id,
                    Filename = attachment.Filename,
                    Url = attachment.Url,
                    ProxyUrl = attachment.ProxyUrl,
                    Size = attachment.Size,
                    Height = attachment.Height,
                    Width = attachment.Width,
                    Ephemeral = attachment.Ephemeral,
                    Description = attachment.Description,
                    ContentType = attachment.ContentType
                };
                message.Attachments.Add(convertedAttachment);
            }

            return message;
        }
    }
}
