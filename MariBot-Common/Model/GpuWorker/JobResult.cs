using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public class JobResult
    {
        public string? FileName { get; set; }
        public byte[]? Data { get; set; }
        public string? Message { get; set; }
    }
}
