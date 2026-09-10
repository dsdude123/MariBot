using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Responses;
using MariBot.Core.Models.Config;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Wolfram Alpha queries, for the one command that uses them.
    /// </summary>
    /// <remarks>
    /// The client is built defensively because a bad Wolfram key used to stop the
    /// whole bot from starting. Genbox.WolframAlpha validates the app id in its
    /// constructor and throws on anything that is not exactly 17 characters —
    /// including the "SET-ME" placeholder that ships in appsettings.json — and
    /// Discord.Net resolves every module's dependencies while it builds the
    /// command list at startup. So that throw came out of host startup, and a
    /// fresh deployment died before it ever reached Discord, over an optional
    /// credential for a single command.
    /// <para>
    /// Now an unusable key costs you the one command that needs it.
    /// </para>
    /// </remarks>
    public class WolframAlphaService
    {
        private readonly ILogger<WolframAlphaService> logger;
        private readonly WolframAlphaClient? client;

        public WolframAlphaService(ILogger<WolframAlphaService> logger, IConfiguration configuration)
        {
            this.logger = logger;

            var appId = configuration["DiscordSettings:WolframAlphaAppId"];

            if (ConfiguredValue.IsUnset(appId))
            {
                logger.LogWarning(
                    "DiscordSettings:WolframAlphaAppId is not set. The Wolfram Alpha command is disabled.");
                return;
            }

            try
            {
                client = new WolframAlphaClient(appId);
            }
            catch (Exception ex)
            {
                // Caught rather than length-checked here: the library owns what a
                // valid app id looks like, and duplicating its rule would start
                // rejecting keys it accepts the moment that rule changes.
                logger.LogError(
                    "DiscordSettings:WolframAlphaAppId was rejected: {Message}. The Wolfram Alpha command is disabled.",
                    ex.Message);
            }
        }

        /// <summary>
        /// Whether a usable app id was configured. False means
        /// <see cref="QuerySimple"/> will not work and callers should say so
        /// rather than call it.
        /// </summary>
        public bool IsConfigured => client != null;

        public async Task<FullResultResponse> QuerySimple(string query)
        {
            if (client == null)
            {
                throw new InvalidOperationException(
                    "Wolfram Alpha is not configured. Set DiscordSettings:WolframAlphaAppId.");
            }

            return await client.FullResultAsync(query);
        }
    }
}
