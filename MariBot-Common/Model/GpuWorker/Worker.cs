using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public class Worker
    {
        public string Endpoint { get; set; }
        public WorkerCapability[] Capabilities { get; set; }
        public WorkerStatus Status { get; set; }
        public Guid CurrentJob { get; set; }
    }
}
