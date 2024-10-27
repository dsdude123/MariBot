using IGDB;
using IGDB.Models;

namespace MariBot.Core.Services
{
    public class IgdbService
    {
        private readonly IGDBClient igdbClient;

        public IgdbService(IConfiguration configuration) {
            igdbClient = new IGDBClient(
                configuration["DiscordSettings:IGDBClientId"],
                configuration["DiscordSettings:IGDBClientSecret"]);
        }

        public async Task<Game?> SearchGame(string searchString)
        {
            var games = await igdbClient.QueryAsync<Game>(IGDBClient.Endpoints.Games, $"search \"{searchString}\"; fields id,name");
            return games.FirstOrDefault();
        } 
    }
}
