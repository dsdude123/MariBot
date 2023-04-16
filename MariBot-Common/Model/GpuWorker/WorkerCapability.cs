using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public enum WorkerCapability
    {
        CPU,
        ConsumerGPU, // 8 GB VRAM
        DatacenterGPU // 24 GB VRAM
    }
}
