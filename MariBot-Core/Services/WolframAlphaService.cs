using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Responses;

namespace MariBot.Core.Services
{
    public class WolframAlphaService
    {
        private readonly WolframAlphaClient client;

        public WolframAlphaService(IConfiguration configuration)
        {
            client = new WolframAlphaClient(configuration["DiscordSettings:WolframAlphaAppId"]);
        }

        public async Task<FullResultResponse> QuerySimple(string query)
        {
            return await client.FullResultAsync(query);
        }
    }
}
