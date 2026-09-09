using MariBot.Common.Model.GpuWorker;
using Microsoft.AspNetCore.Mvc;

namespace MariBot.Worker.Controllers
{
    [Route("api/worker")]
    [ApiController]
    public class WorkerController : ControllerBase
    {
        private readonly ILogger<WorkerController> logger;
        private readonly JobHandler jobHandler;

        public WorkerController(ILogger<WorkerController> logger, JobHandler jobHandler)
        {
            this.logger = logger;
            this.jobHandler = jobHandler;
        }

        /// <summary>
        /// This worker's status. Core polls it; the container healthcheck uses it too.
        /// </summary>
        [HttpGet]
        public WorkerStatus Get()
        {
            return WorkerGlobals.WorkerStatus;
        }

        /// <summary>
        /// Accepts a job, if this worker is free.
        /// </summary>
        [HttpPost]
        public IActionResult Post([FromBody] WorkerJob value)
        {
            if (value == null)
            {
                return BadRequest();
            }

            // Where to post the result. The worker answers whoever called it, so
            // Core does not have to know its own reachable address.
            value.ReturnHost = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost";

            if (value.ReturnHost.Equals("::1"))
            {
                value.ReturnHost = "localhost";
            }

            // Claiming the slot is what decides this, not a preceding status
            // read: two jobs arriving together must not both be accepted.
            if (!WorkerGlobals.TryAccept(value))
            {
                return StatusCode(503);
            }

            WorkerGlobals.Job = value;

            // Task.Run carries the ambient job into the worker task; the request
            // returns as soon as the job is accepted.
            Task.Run(jobHandler.HandleJob);

            return Accepted();
        }

        /// <summary>
        /// Gives up on a job Core has stopped waiting for, freeing this worker
        /// to take new work.
        /// </summary>
        /// <remarks>
        /// The job itself cannot be stopped — an ImageMagick call in progress
        /// runs to completion — but its result is discarded when it finishes and
        /// finds it no longer owns the slot. Without this, one job that ran long
        /// enough for Core to give up would keep the worker busy until it
        /// happened to finish.
        /// </remarks>
        [HttpDelete("job/{jobId:guid}")]
        public IActionResult Abandon(Guid jobId)
        {
            if (!WorkerGlobals.Abandon(jobId))
            {
                return NotFound();
            }

            logger.LogWarning("Core abandoned job {JobId}; this worker is free again.", jobId);
            return NoContent();
        }
    }
}
