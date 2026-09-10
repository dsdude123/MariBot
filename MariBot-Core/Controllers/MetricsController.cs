using MariBot.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariBot.Core.Controllers
{
    /// <summary>
    /// Job metrics: how long commands take, which get used, and how often they
    /// fail.
    /// </summary>
    /// <remarks>
    /// The page is for reading now; the JSON is the same report in a shape
    /// something else can consume, which is the seam a real metrics stack would
    /// attach to later.
    /// </remarks>
    public class MetricsController : Controller
    {
        /// <summary>
        /// Longest window that can be asked for, so a bad query string cannot
        /// pull the whole retention period into memory.
        /// </summary>
        private static readonly TimeSpan MaxWindow = TimeSpan.FromDays(30);

        private static readonly TimeSpan DefaultWindow = TimeSpan.FromHours(24);

        private readonly JobMetricsService metrics;

        public MetricsController(JobMetricsService metrics)
        {
            this.metrics = metrics;
        }

        [Route("metrics")]
        [HttpGet]
        public IActionResult Index(int? hours)
        {
            return View(metrics.Report(WindowFrom(hours), DateTimeOffset.UtcNow));
        }

        [Route("metrics/json")]
        [HttpGet]
        public IActionResult Json(int? hours)
        {
            return Ok(metrics.Report(WindowFrom(hours), DateTimeOffset.UtcNow));
        }

        private static TimeSpan WindowFrom(int? hours)
        {
            if (!hours.HasValue || hours.Value <= 0)
            {
                return DefaultWindow;
            }

            var requested = TimeSpan.FromHours(hours.Value);
            return requested > MaxWindow ? MaxWindow : requested;
        }
    }
}
