using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MariBot.Core.Controllers
{
    [Route("job")]
    [ApiController]
    public class WorkerJobController : ControllerBase
    {
        private WorkerManagerService workerManagerService { get; set; }

        [HttpPost]
        public void Post([FromBody] WorkerJob job)
        {
            workerManagerService.ReturnResult(job);
        }
    }
}
