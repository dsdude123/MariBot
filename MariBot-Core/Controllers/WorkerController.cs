using MariBot.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariBot.Core.Controllers
{
    /// <summary>
    /// The admin page for the worker pool: what is registered, what each one is
    /// doing, and the hold/ready switch for taking one out of rotation by hand.
    /// </summary>
    public class WorkerController : Controller
    {
        private readonly WorkerManagerService workerManagerService;

        public WorkerController(WorkerManagerService workerManagerService)
        {
            this.workerManagerService = workerManagerService;
        }

        [Route("workers")]
        [HttpGet]
        public ActionResult Index()
        {
            return View(workerManagerService.Workers);
        }

        /// <summary>
        /// Takes a worker out of rotation, for when the machine is wanted for
        /// something else. Survives the worker restarting.
        /// </summary>
        /// <remarks>
        /// Addressed by registry id rather than endpoint: endpoints are now
        /// self-reported and can change under a worker between page loads.
        /// </remarks>
        [Route("workers/hold")]
        [HttpGet]
        public IActionResult Hold(Guid id)
        {
            workerManagerService.HoldWorker(id);
            return RedirectToAction("Index");
        }

        [Route("workers/ready")]
        [HttpGet]
        public IActionResult Ready(Guid id)
        {
            workerManagerService.ReadyWorker(id);
            return RedirectToAction("Index");
        }
    }
}
