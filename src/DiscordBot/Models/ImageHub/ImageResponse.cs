using MariBot.Models.TalkHub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.ImageHub
{
    public class ImageResponse
    {
        public Guid RequestId { get; set; }

        public RequestStatus Status { get; set; }

        public ErrorDetail? ErrorDetail { get; set; }

        public DateTime? AcceptanceTime { get; set; }

        public DateTime? ProcessingStartTime { get; set; }

        public DateTime? ProcessingEndTime { get; set; }

        public uint EstimatedDeliveryTimeSeconds { get; set; }
    }
}
