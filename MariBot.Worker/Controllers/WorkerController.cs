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
        // GET: api/<WorkerController>
        [HttpGet]
        public WorkerStatus Get()
        {
            return WorkerGlobals.WorkerStatus;
        }
        
        // POST api/<WorkerController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WorkerJob value)
        {
            if (WorkerGlobals.WorkerStatus != WorkerStatus.Ready)
            {
                return StatusCode(503);
            }

            WorkerGlobals.WorkerStatus = WorkerStatus.Working;
            WorkerGlobals.Job = value;

            Task.Run(JobHandler.HandleJob);

            return Accepted();
        }
    }
}
