using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public class WorkerJob
    {
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public Command Command { get; set; }
        public byte[]? SourceImage { get; set; }
        public string? SourceText { get; set; }
        public JobResult? Result { get; set; }
    }
}
