using IGDB;
using IGDB.Models;

namespace MariBot.Core.Services
{
    public class IgdbService
    {
        private readonly IGDBClient igdbClient;
        private readonly ILogger<IgdbService> logger;

        public IgdbService(IConfiguration configuration, ILogger<IgdbService> logger) {
            this.logger = logger;
            igdbClient = new IGDBClient(
                configuration["DiscordSettings:IGDBClientId"],
                configuration["DiscordSettings:IGDBClientSecret"]);
        }

        public async Task<Game?> SearchGame(string searchString)
        {
            var games = await igdbClient.QueryAsync<Game>(IGDBClient.Endpoints.Games, $"search \"{searchString}\"; fields id,name;");
            logger.LogInformation($"IGDB search for {searchString} returned {games.Length} results");
            return games.FirstOrDefault();
        } 
    }
}
