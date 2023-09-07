using MariBot.Core.Models.Yahoo;
using MariBot.Core.Models.Yahoo.FantasySports;
using RestSharp;
using RestSharp.Authenticators;

namespace MariBot.Core.Services
{
    public class YahooFantasyService
    {
        private static OAuthResponse authDetails;
        private static DateTime expiresTime = DateTime.MaxValue;

        private readonly string clientId;
        private readonly string clientSecret;

        public YahooFantasyService(IConfiguration configuration)
        {
            clientId = configuration["DiscordSettings:YahooFantasyClientId"];
            clientSecret = configuration["DiscordSettings:YahooFantasyClientSecret"];
        }

        public string GetAuthLink()
        {
            return
                $"https://api.login.yahoo.com/oauth2/request_auth?client_id={clientId}&redirect_uri=oob&response_type=code&language=en-us";
        }

        public async Task Authorize(string authorizationCode)
        {
            var client = new RestClient();
            client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret);

            var request = new RestRequest("https://api.login.yahoo.com/oauth2/get_token");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"redirect_uri=oob&code={authorizationCode}&grant_type=authorization_code", 
                ParameterType.RequestBody);
            authDetails = await client.PostAsync<OAuthResponse>(request);
            expiresTime = DateTime.Now.AddSeconds(authDetails.expiresIn);
        }

        public async Task<string> GetRaw(string url)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest(url, Method.GET);
            IRestResponse res = await client.ExecuteAsync(request);

            return res.Content;
        }

        public async Task<FantasyContent> GetLeague(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetStandings(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/standings");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetScoreboard(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/scoreboard");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task<FantasyContent> GetTransactions(string leagueId)
        {
            await RefreshToken();
            var client = new RestClient();
            client.Authenticator = new JwtAuthenticator(authDetails.accessToken);

            var request = new RestRequest($"https://fantasysports.yahooapis.com/fantasy/v2/league/{leagueId}/transactions");
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            return await client.GetAsync<FantasyContent>(request);
        }

        public async Task RefreshToken()
        {
            if (expiresTime < DateTime.Now)
            {
                var client = new RestClient();
                client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret);

                var request = new RestRequest("https://api.login.yahoo.com/oauth2/get_token");
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("application/x-www-form-urlencoded", $"redirect_uri=oob&refresh_token={authDetails.refreshToken}&grant_type=refresh_token",
                    ParameterType.RequestBody);
                authDetails = await client.PostAsync<OAuthResponse>(request);
                expiresTime = DateTime.Now.AddSeconds(authDetails.expiresIn);
                if (authDetails.accessToken == null)
                {
                    throw new UnauthorizedAccessException("Failed to authenticate to Yahoo");
                }
            }
        }
    }
}
