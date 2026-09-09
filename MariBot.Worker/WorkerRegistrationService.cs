using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using MariBot.Common.Model.GpuWorker;
using Newtonsoft.Json;

namespace MariBot.Worker
{
    /// <summary>
    /// Registers this worker with Core on startup, heartbeats to stay
    /// registered, and deregisters on shutdown.
    /// </summary>
    /// <remarks>
    /// Core used to learn about workers from a checked-in file, which meant a
    /// decommissioned machine stayed in the list and a new one needed an edit
    /// and a restart. Turning it around — the worker announces itself, and
    /// stops being listed when it stops talking — removes both.
    /// <para>
    /// Core being down is expected rather than exceptional: the two restart
    /// independently, so this retries indefinitely and re-registers whenever
    /// Core says it does not know us, which is what happens when Core restarts.
    /// </para>
    /// </remarks>
    public class WorkerRegistrationService : BackgroundService
    {
        private readonly ILogger<WorkerRegistrationService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly WorkerSettings settings;
        private readonly IServer server;

        private Guid workerId;
        private TimeSpan heartbeatInterval = TimeSpan.FromSeconds(15);

        public WorkerRegistrationService(
            ILogger<WorkerRegistrationService> logger,
            IHttpClientFactory httpClientFactory,
            WorkerSettings settings,
            IServer server)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.settings = settings;
            this.server = server;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!settings.IsRegistrationConfigured)
            {
                logger.LogWarning(
                    "{Section}:CoreEndpoint and {Section}:PreSharedKey are not both set; this worker will not register " +
                    "with Core and will only run jobs posted to it directly.", WorkerSettings.SectionName);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (workerId == Guid.Empty)
                {
                    if (!await TryRegister(stoppingToken))
                    {
                        await Delay(TimeSpan.FromSeconds(Math.Max(1, settings.RegistrationRetrySeconds)), stoppingToken);
                        continue;
                    }
                }

                await Delay(heartbeatInterval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (!await TryHeartbeat(stoppingToken))
                {
                    // Core has forgotten us — it restarted, or evicted us for
                    // being quiet. Registering again is the whole recovery.
                    workerId = Guid.Empty;
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await Deregister();
            await base.StopAsync(cancellationToken);
        }

        private async Task<bool> TryRegister(CancellationToken cancellationToken)
        {
            var endpoint = ResolveAdvertisedEndpoint();
            if (endpoint == null)
            {
                logger.LogError(
                    "Cannot work out an address to advertise. Set {Section}:AdvertisedEndpoint.",
                    WorkerSettings.SectionName);
                return false;
            }

            var registration = new WorkerRegistration
            {
                Name = string.IsNullOrWhiteSpace(settings.Name) ? Dns.GetHostName() : settings.Name,
                Endpoint = endpoint,
                Capabilities = settings.Capabilities
            };

            try
            {
                using var http = CreateClient();
                var payload = new StringContent(
                    JsonConvert.SerializeObject(registration), Encoding.UTF8, "application/json");

                var response = await http.PostAsync("/api/workers/register", payload, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Core refused this worker's registration: {Status} {Reason}.",
                        (int)response.StatusCode, response.ReasonPhrase);
                    return false;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<WorkerRegistrationResult>(body);

                if (result == null || result.WorkerId == Guid.Empty)
                {
                    logger.LogError("Core accepted the registration but returned no worker id.");
                    return false;
                }

                workerId = result.WorkerId;

                if (result.HeartbeatSeconds > 0)
                {
                    heartbeatInterval = TimeSpan.FromSeconds(result.HeartbeatSeconds);
                }

                logger.LogInformation(
                    "Registered with Core as {WorkerId} ({Name} at {Endpoint}); heartbeating every {Interval}.",
                    workerId, registration.Name, endpoint, heartbeatInterval);

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not reach Core to register: {Message}. Retrying.", ex.Message);
                return false;
            }
        }

        private async Task<bool> TryHeartbeat(CancellationToken cancellationToken)
        {
            try
            {
                using var http = CreateClient();
                var response = await http.PostAsync($"/api/workers/{workerId}/heartbeat", null, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logger.LogWarning("Core no longer knows this worker; registering again.");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Heartbeat rejected: {Status} {Reason}.",
                        (int)response.StatusCode, response.ReasonPhrase);
                    return response.StatusCode != HttpStatusCode.Unauthorized;
                }

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return true;
            }
            catch (Exception ex)
            {
                // A missed heartbeat is not the same as being deregistered.
                // Core allows several before it acts, so keep the id and retry.
                logger.LogWarning("Heartbeat to Core failed: {Message}.", ex.Message);
                return true;
            }
        }

        private async Task Deregister()
        {
            if (workerId == Guid.Empty || !settings.IsRegistrationConfigured)
            {
                return;
            }

            try
            {
                using var http = CreateClient();
                await http.DeleteAsync($"/api/workers/{workerId}");
                logger.LogInformation("Deregistered from Core.");
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not deregister cleanly: {Message}. Core will time this worker out.",
                    ex.Message);
            }
        }

        private HttpClient CreateClient()
        {
            var http = httpClientFactory.CreateClient();
            http.BaseAddress = new Uri(settings.CoreEndpoint!);
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.Add("X-Worker-Key", settings.PreSharedKey);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return http;
        }

        /// <summary>
        /// The address to hand Core: configured if set, otherwise the hostname
        /// and the port Kestrel actually bound.
        /// </summary>
        private string? ResolveAdvertisedEndpoint()
        {
            if (!string.IsNullOrWhiteSpace(settings.AdvertisedEndpoint))
            {
                return settings.AdvertisedEndpoint!.TrimEnd('/');
            }

            var port = BoundHttpPort();
            return port.HasValue ? $"http://{Dns.GetHostName()}:{port}" : null;
        }

        /// <summary>
        /// The port Kestrel is listening on, read after startup rather than
        /// guessed, so it is right whether the port came from ASPNETCORE_URLS,
        /// a launch profile, or a default.
        /// </summary>
        private int? BoundHttpPort()
        {
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;

            foreach (var address in addresses ?? Enumerable.Empty<string>())
            {
                // Kestrel reports wildcard binds as http://[::]:8092 or
                // http://+:8092, neither of which parses as a host we could
                // advertise — but the port is what we are after.
                var portText = address.Split(':').LastOrDefault()?.TrimEnd('/');
                if (int.TryParse(portText, out var port))
                {
                    return port;
                }
            }

            return null;
        }

        private static async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
        }
    }
}
