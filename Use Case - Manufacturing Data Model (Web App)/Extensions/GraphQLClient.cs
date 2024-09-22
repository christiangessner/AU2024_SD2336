using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

public class GraphQLClient
{
    private readonly string _endpoint;
    private readonly RestClient _client;

    public GraphQLClient(string endpoint)
    {
        _endpoint = endpoint;
        var options = new RestClientOptions("https://developer.api.autodesk.com") 
        { 
            Timeout = Timeout.InfiniteTimeSpan,
        };
        _client = new RestClient(options, configureSerialization: s => s.UseNewtonsoftJson());
    }

    public async Task<T?> Query<T>(string query, dynamic variables, string bearerToken)
    {
        var request = new RestRequest(_endpoint, Method.Post); //"beta/graphql"

        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {bearerToken}");

        query = query.Replace(Environment.NewLine, "\\n");
        query = query.Replace("\"", "\\\"");

        var variableJson = JsonConvert.SerializeObject(variables, Formatting.Indented);
        var json = $$"""{ "query": "{{query}}", "variables": {{variableJson}} }""";

        request.AddParameter("application/json", json, ParameterType.RequestBody);
        var response = await _client.ExecuteAsync<T>(request);
        if (response.IsSuccessStatusCode)
        {
            if (response != null && response.Data != null)
                return response.Data;
        }

        return default;
    }
}