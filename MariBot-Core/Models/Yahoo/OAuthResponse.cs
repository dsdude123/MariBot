using System.Text.Json.Serialization;

namespace MariBot.Core.Models.Yahoo
{
    public class OAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string accessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string tokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public uint expiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string refreshToken { get; set; }

        [JsonPropertyName("xoauth_yahoo_guid")]
        [Obsolete("This claim is deprecated. If you need the user’s GUID value, please use the OpenID Connect flows. The GUID will be provided in the id_token.", false)]
        public string yahooUserGuid { get; set; }
    }
}
