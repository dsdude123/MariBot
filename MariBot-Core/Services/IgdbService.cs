using IGDB;
using IGDB.Models;
using MariBot.Core.Models.Config;

namespace MariBot.Core.Services
{
    public class IgdbService
    {
        // Null when no usable credentials were configured. IGDBClient throws on a
        // missing client id in its constructor, and Discord.Net resolves this
        // service while building its command list at startup, so an absent id
        // used to stop the whole bot rather than the one lookup that needs it.
        private readonly IGDBClient? igdbClient;
        private readonly ILogger<IgdbService> logger;

        public IgdbService(IConfiguration configuration, ILogger<IgdbService> logger) {
            this.logger = logger;

            var clientId = configuration["DiscordSettings:IGDBClientId"];
            var clientSecret = configuration["DiscordSettings:IGDBClientSecret"];

            if (ConfiguredValue.IsUnset(clientId) || ConfiguredValue.IsUnset(clientSecret))
            {
                logger.LogWarning(
                    "DiscordSettings:IGDBClientId and IGDBClientSecret are not both set. IGDB lookups are disabled.");
                return;
            }

            try
            {
                igdbClient = new IGDBClient(clientId, clientSecret);
            }
            catch (Exception ex)
            {
                logger.LogError("IGDB credentials were rejected: {Message}. IGDB lookups are disabled.", ex.Message);
            }
        }

        /// <summary>
        /// Whether usable credentials were configured.
        /// </summary>
        public bool IsConfigured => igdbClient != null;

        /// <summary>
        /// Looks up a game by name.
        /// </summary>
        /// <returns>
        /// The best match, or null when there is none — including when IGDB is
        /// not configured. Null rather than a throw because every caller already
        /// handles "no match", and the reason is in the log either way.
        /// </returns>
        public async Task<Game?> SearchGame(string searchString)
        {
            if (igdbClient == null)
            {
                logger.LogWarning(
                    "IGDB lookup for {Search} skipped: no credentials configured.", searchString);
                return null;
            }

            var games = await igdbClient.QueryAsync<Game>(IGDBClient.Endpoints.Games, $"search \"{searchString}\"; fields id,name;");
            logger.LogInformation($"IGDB search for {searchString} returned {games.Length} results");
            return games.FirstOrDefault();
        } 
    }
}
