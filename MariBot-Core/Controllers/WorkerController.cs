using MariBot.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MariBot.Core.Controllers
{
    [Route("workers")]
    public class WorkerController : Controller
    {
        private WorkerManagerService workerManagerService { get; set; }

        public WorkerController(WorkerManagerService workerManagerService)
        {
            this.workerManagerService = workerManagerService;
        }

        // GET: WorkerController
        [HttpGet]
        public ActionResult Index()
        {
            return View(workerManagerService.Workers);
        }

        [HttpGet("hold")]
        public IActionResult Hold(string endpoint)
        {
            workerManagerService.HoldWorker(endpoint);
            return RedirectToAction("Index");
        }

        [HttpGet("ready")]
        public IActionResult Ready(string endpoint)
        {
            workerManagerService.ReadyWorker(endpoint);
            return RedirectToAction("Index");
        }
    }
}
