using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.TalkHub
{
    public enum RequestStatus
    {
        Accepted,
        QueuedAtProvider,
        Done,
        Failure
    }
}
