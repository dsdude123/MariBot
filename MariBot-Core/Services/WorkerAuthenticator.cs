using System.Security.Cryptography;
using System.Text;
using MariBot.Core.Models.Config;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Checks the pre-shared key a worker presents when it registers or checks in.
    /// </summary>
    /// <remarks>
    /// Registration lets a caller put itself in line for other people's images,
    /// so it is not open. The check fails closed: with no key configured no
    /// worker can register, rather than every caller being able to.
    /// </remarks>
    public class WorkerAuthenticator
    {
        /// <summary>Header a worker presents its key in.</summary>
        public const string HeaderName = "X-Worker-Key";

        private readonly ILogger<WorkerAuthenticator> logger;
        private readonly byte[]? expectedKey;

        public WorkerAuthenticator(ILogger<WorkerAuthenticator> logger, WorkerSettings settings)
        {
            this.logger = logger;

            if (ConfiguredValue.IsUnset(settings.PreSharedKey))
            {
                logger.LogWarning(
                    "{Section}:PreSharedKey is not set, so no worker can register. Set it on Core and on every worker.",
                    WorkerSettings.SectionName);
                expectedKey = null;
            }
            else
            {
                expectedKey = Encoding.UTF8.GetBytes(settings.PreSharedKey);
            }
        }

        /// <summary>Whether registration is possible at all in this deployment.</summary>
        public bool IsConfigured => expectedKey != null;

        /// <summary>
        /// Whether <paramref name="request"/> carries the right key.
        /// </summary>
        public bool IsAuthorised(HttpRequest request)
        {
            if (expectedKey == null)
            {
                return false;
            }

            if (!request.Headers.TryGetValue(HeaderName, out var presented) || presented.Count != 1)
            {
                return false;
            }

            var presentedKey = Encoding.UTF8.GetBytes(presented[0] ?? string.Empty);

            // Fixed-time: a length-independent comparison stops the key being
            // recovered a character at a time from response timings.
            return CryptographicOperations.FixedTimeEquals(presentedKey, expectedKey);
        }
    }
}
