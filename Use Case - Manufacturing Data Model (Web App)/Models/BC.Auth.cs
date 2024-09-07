using System.Net;
using Newtonsoft.Json;

public partial class BC
{
    public class Token
    {
        public required string AccessToken;
        public required string TokenType;
    }

    private readonly HttpClient HttpClient = new HttpClient();

    private OAuthToken? _token;

    public Token GetToken()
    {

        if (_token == null || _token.Issued.AddSeconds(_token.ExpiresIn) < DateTime.Now.AddMinutes(-1))
            _token = Task.Run(async () => await GetOAuthToken(_tenantId, _clientId, _clientSecret)).Result;

        return new Token
        {
            AccessToken = _token.AccessToken,
            TokenType = _token.TokenType
        };
    }

    private async Task<OAuthToken> GetOAuthToken(string tenantId, string clientId, string clientSecret)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        var body = new Dictionary<string, string>{
            { "client_id", clientId },
            { "scope", "https://api.businesscentral.dynamics.com/.default" },
            { "client_secret", clientSecret },
            { "grant_type", "client_credentials" }
        };
        var content = new FormUrlEncodedContent(body);
        request.Content = content;
        var response = await HttpClient.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new Exception($"Error getting token: {await response.Content.ReadAsStringAsync()}");

        var responseJson = await response.Content.ReadAsStringAsync();
        var token = JsonConvert.DeserializeObject<OAuthToken>(responseJson);
        if (token == null)
            throw new Exception("Error deserializing token");

        token.Issued = DateTime.Now;
        return token;
    }

    #region Serialization Classes
    public class OAuthToken
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; } = "";
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonProperty("ext_expires_in")]
        public int ExtExpiresIn { get; set; }
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = "";

        public DateTime Issued { get; set; }
    }
    #endregion
}
