using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Models.Config;
using MariBot.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariBot.Core.Controllers
{
    /// <summary>
    /// Where workers announce themselves and check in.
    /// </summary>
    /// <remarks>
    /// This replaces worker-config.json. A worker knows its own address, its own
    /// capabilities, and — unlike a file written months ago — whether it still
    /// exists, so it is the one that says so.
    /// </remarks>
    [ApiController]
    [Route("api/workers")]
    public class WorkerRegistrationController : ControllerBase
    {
        private readonly ILogger<WorkerRegistrationController> logger;
        private readonly WorkerRegistry registry;
        private readonly WorkerAuthenticator authenticator;
        private readonly WorkerSettings settings;

        public WorkerRegistrationController(
            ILogger<WorkerRegistrationController> logger,
            WorkerRegistry registry,
            WorkerAuthenticator authenticator,
            WorkerSettings settings)
        {
            this.logger = logger;
            this.registry = registry;
            this.authenticator = authenticator;
            this.settings = settings;
        }

        /// <summary>
        /// Registers a worker, or refreshes an existing registration for the
        /// same endpoint.
        /// </summary>
        [HttpPost("register")]
        public ActionResult<WorkerRegistrationResult> Register([FromBody] WorkerRegistration registration)
        {
            if (!authenticator.IsAuthorised(Request))
            {
                logger.LogWarning("Rejected a worker registration for {Endpoint} with a bad or missing key.",
                    registration?.Endpoint);
                return Unauthorized();
            }

            if (registration == null || string.IsNullOrWhiteSpace(registration.Endpoint))
            {
                return BadRequest("Endpoint is required.");
            }

            if (!Uri.TryCreate(registration.Endpoint, UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("Endpoint must be an absolute http or https URL.");
            }

            if (!TryParseCapabilities(registration.Capabilities, out var capabilities, out var unknown))
            {
                // Naming a capability this Core has never heard of is worth
                // saying out loud: silently registering with none would leave a
                // worker that is up, healthy, and never sent anything.
                return BadRequest($"Unknown worker capability '{unknown}'.");
            }

            if (capabilities.Length == 0)
            {
                return BadRequest("At least one capability is required.");
            }

            var worker = registry.Register(registration, capabilities, DateTimeOffset.UtcNow);

            return Ok(new WorkerRegistrationResult
            {
                WorkerId = worker.Id,
                HeartbeatSeconds = (int)settings.Heartbeat.TotalSeconds,
                EvictAfterSeconds = (int)settings.EvictAfter.TotalSeconds
            });
        }

        /// <summary>
        /// Keeps a registration alive.
        /// </summary>
        /// <returns>
        /// 404 when Core does not know this worker, which is the worker's cue to
        /// register again — that is how a worker recovers from Core restarting.
        /// </returns>
        [HttpPost("{workerId:guid}/heartbeat")]
        public IActionResult Heartbeat(Guid workerId)
        {
            if (!authenticator.IsAuthorised(Request))
            {
                return Unauthorized();
            }

            return registry.Heartbeat(workerId, DateTimeOffset.UtcNow) ? NoContent() : NotFound();
        }

        /// <summary>
        /// Removes a worker that is shutting down, rather than waiting for it to
        /// time out of the registry.
        /// </summary>
        [HttpDelete("{workerId:guid}")]
        public IActionResult Deregister(Guid workerId)
        {
            if (!authenticator.IsAuthorised(Request))
            {
                return Unauthorized();
            }

            return registry.Deregister(workerId) ? NoContent() : NotFound();
        }

        /// <summary>
        /// The registry as JSON, for checking what Core thinks is out there
        /// without loading the admin page.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<object>> List()
        {
            return Ok(registry.Snapshot().Select(w => new
            {
                w.Id,
                w.Name,
                w.Endpoint,
                Capabilities = w.Capabilities.Select(c => c.ToString()),
                Status = w.Status.ToString(),
                Circuit = w.Breaker.State.ToString(),
                w.Breaker.ConsecutiveFailures,
                w.Breaker.Trips,
                w.JobsCompleted,
                w.JobsFailed,
                w.LastHeartbeat,
                w.RegisteredAt,
                CurrentJob = w.CurrentJob == Guid.Empty ? null : w.CurrentJob.ToString()
            }));
        }

        private static bool TryParseCapabilities(string[]? names, out WorkerCapability[] capabilities, out string? unknown)
        {
            capabilities = Array.Empty<WorkerCapability>();
            unknown = null;

            if (names == null)
            {
                return true;
            }

            var parsed = new List<WorkerCapability>();
            foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                if (!Enum.TryParse<WorkerCapability>(name.Trim(), ignoreCase: true, out var capability)
                    || !Enum.IsDefined(capability))
                {
                    unknown = name;
                    return false;
                }

                if (!parsed.Contains(capability))
                {
                    parsed.Add(capability);
                }
            }

            capabilities = parsed.ToArray();
            return true;
        }
    }
}
