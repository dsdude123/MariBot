using RestSharp;
using MariBot.Core.Models.BlackForestLabs.Flux;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Image generation through Black Forest Labs.
    /// </summary>
    /// <remarks>
    /// Two models are reachable: FLUX.2 [max] for the `flux` command, and
    /// FLUX 1.1 [pro] for `flux1`. The older one is kept because it is still
    /// supported and cheaper, not as a fallback.
    /// <para>
    /// Both live on api.bfl.ai. The api.bfl.ml host this used to call is BFL's
    /// old domain.
    /// </para>
    /// </remarks>
    public class FluxService
    {
        public const string ApiBase = "https://api.bfl.ai/v1";

        /// <summary>FLUX.2 [max] — the current top-tier model.</summary>
        public const string Flux2MaxEndpoint = $"{ApiBase}/flux-2-max";

        /// <summary>FLUX 1.1 [pro] — the previous generation, still supported.</summary>
        public const string Flux1ProEndpoint = $"{ApiBase}/flux-pro-1.1";

        public const string ResultEndpoint = $"{ApiBase}/get_result";

        private readonly string? apiKey;
        private readonly ILogger<FluxService> logger;

        /// <summary>For mocking in tests, matching DataService's seam.</summary>
        protected FluxService() { logger = null!; }

        public FluxService(IConfiguration configuration, ILogger<FluxService> logger)
        {
            apiKey = configuration["DiscordSettings:BlackForestLabsApiKey"];
            this.logger = logger;
        }

        /// <summary>
        /// Generates an image with FLUX.2 [max].
        /// </summary>
        public virtual async Task<ImageResponse.Result> GenerateFlux(string prompt)
        {
            var imageRequest = new Flux2ImageRequest
            {
                width = 1440,
                height = 1440,
                output_format = "jpeg",
                // Left on, matching what prompt_upsampling = true asked for on
                // the older model.
                disable_pup = false,
                safety_tolerance = 2,
                prompt = prompt
            };

            return await Generate(Flux2MaxEndpoint, imageRequest);
        }

        /// <summary>
        /// Generates an image with FLUX 1.1 [pro], the generation before FLUX.2.
        /// </summary>
        public virtual async Task<ImageResponse.Result> GenerateFlux1(string prompt)
        {
            var imageRequest = new ImageRequest
            {
                width = 1440,
                height = 1440,
                output_format = "jpeg",
                prompt_upsampling = true,
                safety_tolerance = 2,
                prompt = prompt
            };

            return await Generate(Flux1ProEndpoint, imageRequest);
        }

        private async Task<ImageResponse.Result> Generate(string endpoint, object imageRequest)
        {
            var client = new RestClient();
            client.AddDefaultHeader("X-Key", apiKey);
            client.AddDefaultHeader("Content-Type", "application/json");

            var rest = new RestRequest(endpoint, Method.POST);
            rest.AddJsonBody(imageRequest);

            var initialResponse = await client.ExecuteAsync<ImageResponse>(rest);

            var retryCount = 0;

            while (initialResponse.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", initialResponse.StatusCode.ToString());
                initialResponse = await client.ExecuteAsync<ImageResponse>(rest);
                await Task.Delay(TimeSpan.FromSeconds(10));
                retryCount++;
            }

            retryCount = 0;

            if (initialResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return ReturnDiscordError(initialResponse.StatusCode.ToString());
            }

            await Task.Delay(TimeSpan.FromSeconds(5));

            rest = new RestRequest(ResultEndpoint, Method.GET);
            rest.AddQueryParameter("id", initialResponse.Data.id);
            var imageResult = await client.ExecuteAsync<ImageResponse>(rest);

            while (imageResult.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
            {
                logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
                imageResult = await client.ExecuteAsync<ImageResponse>(rest);
                await Task.Delay(TimeSpan.FromSeconds(10));
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
                await Task.Delay(TimeSpan.FromSeconds(1));
                while (imageResult.StatusCode != System.Net.HttpStatusCode.OK && retryCount < 3)
                {
                    logger.LogError("Got bad HTTP status code from BFL: {Code}", imageResult.StatusCode.ToString());
                    imageResult = await client.ExecuteAsync<ImageResponse>(rest);
                    await Task.Delay(TimeSpan.FromSeconds(10));
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
