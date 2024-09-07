using System.Net;
using System.Net.Http.Headers;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

// Page 30: Item Card
// Page 30008: APIV2 - Items
// Page 99000786: Production BOM
// Page 99000788: Lines

public partial class BC
{
    public BC(string clientId, string clientSecret, string tenantId, string environment, string company)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tenantId = tenantId;
        _company = company;

        _baseUrl = $"https://api.businesscentral.dynamics.com/v2.0/{tenantId}/{environment}/ODataV4";
    }

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tenantId;
    private readonly string _company;
    private readonly string _baseUrl;

    // https://restsharp.dev/migration/#restclient-lifecycle
    private RestClient? _client;

    private RestClient GetRestClient()
    {
        if (_client == null)
        {
            // https://learn.microsoft.com/de-de/dynamics365/business-central/dev-itpro/security/security-and-protection
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            // https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/administration/operational-limits-online#ODataServices
            ServicePointManager.DefaultConnectionLimit = 5;

            var options = new RestClientOptions(_baseUrl);
            var client = new RestClient(options, configureSerialization: s => s.UseNewtonsoftJson());
            _client = client;
        }

        return _client;
    }

    private RestRequest GetRestRequest(string url, Method method = Method.Get)
    {
        var token = GetToken();
        var request = new RestRequest(url, method);
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", $"{token.TokenType} {token.AccessToken}");

        return request;
    }

    private async Task<byte[]> GetResourceAsByteArray(string url)
    {
        var token = GetToken();
        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
        var bytes = await HttpClient.GetByteArrayAsync(url);
        return bytes;
    }
}