using MariBot.Core.Models.Yahoo;
using Newtonsoft.Json;
using RestSharp.Authenticators;
using RestSharp;
using MariBot.Core.Models.BlackForestLabs.Flux;

namespace MariBot.Core.Services
{
    public class FluxService
    {

        private string apiKey;
        private ILogger<FluxService> logger;

        public FluxService(IConfiguration configuration, ILogger<FluxService> logger)
        {
            apiKey = configuration["DiscordSettings:BlackForestLabsApiKey"];
            this.logger = logger;
        }

        public async Task<ImageResponse.Result> GenerateFlux(string prompt)
        {
            ImageRequest imageRequest = new ImageRequest();
            imageRequest.width = 1440;
            imageRequest.height = 1440;
            imageRequest.output_format = "jpeg";
            imageRequest.prompt_upsampling = true;
            imageRequest.safety_tolerance = 2;
            imageRequest.prompt = prompt;

            var client = new RestClient();
            client.AddDefaultHeader("X-Key", apiKey);
            client.AddDefaultHeader("Content-Type", "application/json");

            var rest = new RestRequest("https://api.bfl.ml/v1/flux-pro-1.1", Method.POST);
            rest.AddJsonBody(imageRequest);

            var initialResponse = await client.ExecuteAsync<ImageResponse>(rest);

            if (initialResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", initialResponse.StatusCode.ToString());
            } else
            {
                logger.LogInformation("Got ID back from BFL: {Id}", initialResponse.Data.id);
                logger.LogInformation("BFL Body {Body}", initialResponse.Content.ToString());
            }

            Thread.Sleep(5);

            rest = new RestRequest("https://api.bfl.ml/v1/get_result", Method.GET);
            rest.AddQueryParameter("id", initialResponse.Data.id);
            var imageResult = await client.ExecuteAsync<ImageResponse>(rest);

            if (imageResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
            }
            logger.LogInformation("Got generation response, status is {Status}", imageResult.Data.status);
            logger.LogInformation("BFL Body {Body}", imageResult.Content.ToString());

            while (imageResult.Data.status.ToLower().Equals("pending"))
            {
                logger.LogInformation("Request is pending....");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                imageResult = await client.ExecuteAsync<ImageResponse>(rest);
                logger.LogInformation("Pending new status code: {Code}", imageResult.StatusCode.ToString());
                logger.LogInformation("BFL Body {Body}", imageResult.Content.ToString());
                if (imageResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
                    break;
                }
            }

            if (imageResult.Data.status.ToLower().Equals("ready"))
            {
                return imageResult.Data.result;
            }
            else
            {
                logger.LogWarning("Got bad status back from BFL: {Code}", imageResult.Data.status);
            }

            ImageResponse.Result discordError = new ImageResponse.Result();
            discordError.sample = "";
            discordError.prompt = $"Something went wrong. {imageResult.Data.status}";
            return discordError;
        }
    }
}
