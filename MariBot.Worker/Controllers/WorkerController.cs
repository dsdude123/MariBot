using MariBot.Common.Model.GpuWorker;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MariBot.Worker.Controllers
{
    [Route("api/worker")]
    [ApiController]
    public class WorkerController : ControllerBase
    {

        private JobHandler jobHandler;

        public WorkerController(JobHandler jobHandler)
        {
            this.jobHandler = jobHandler;
        }

        [HttpGet]
        public WorkerStatus Get()
        {
            return WorkerGlobals.WorkerStatus;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WorkerJob value)
        {
            value.ReturnHost = HttpContext.Connection.RemoteIpAddress.ToString();

            if (WorkerGlobals.WorkerStatus != WorkerStatus.Ready)
            {
                return StatusCode(503);
            }

            WorkerGlobals.WorkerStatus = WorkerStatus.Working;
            WorkerGlobals.Job = value;

            Task.Run(jobHandler.HandleJob);

            return Accepted();
        }
    }
}
