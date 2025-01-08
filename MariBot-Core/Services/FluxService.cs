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

            var retryCount = 0;

            while (initialResponse.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", initialResponse.StatusCode.ToString());
                initialResponse = await client.ExecuteAsync<ImageResponse>(rest);
                Thread.Sleep(TimeSpan.FromSeconds(10));
                retryCount++;
            }

            retryCount = 0;

            if (initialResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return ReturnDiscordError(initialResponse.StatusCode.ToString());
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));

            rest = new RestRequest("https://api.bfl.ml/v1/get_result", Method.GET);
            rest.AddQueryParameter("id", initialResponse.Data.id);
            var imageResult = await client.ExecuteAsync<ImageResponse>(rest);

            while (imageResult.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
                imageResult = await client.ExecuteAsync<ImageResponse>(rest);
                Thread.Sleep(TimeSpan.FromSeconds(10));
                retryCount++;
            }

            if (imageResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return ReturnDiscordError(imageResult.StatusCode.ToString());
            }

            retryCount = 0;

            while (imageResult.Data.status.ToLower().Equals("pending"))
            {
                logger.LogInformation("Request is pending....");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                while (imageResult.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
                {
                    logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
                    imageResult = await client.ExecuteAsync<ImageResponse>(rest);
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    retryCount++;
                }

                if (imageResult.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return ReturnDiscordError(imageResult.StatusCode.ToString());
                }

                retryCount = 0;
            }

            if (imageResult.Data.status.ToLower().Equals("ready"))
            {
                return imageResult.Data.result;
            }
            else
            {
                logger.LogWarning("Got bad status back from BFL: {Code}", imageResult.Data.status);
            }

            return ReturnDiscordError(imageResult.Data.status);
        }

        public ImageResponse.Result ReturnDiscordError(string error)
        {
            ImageResponse.Result discordError = new ImageResponse.Result();
            discordError.sample = "";
            discordError.prompt = $"Something went wrong. {error}";
            return discordError;
        }
    }
}
