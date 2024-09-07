#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

using Autodesk.SDKManager;
using Autodesk.Authentication.Model;

public class Tokens
{
    public string InternalToken;
    public string PublicToken;
    public string RefreshToken;
    public DateTime ExpiresAt;
}

public partial class APS
{
    private readonly SDKManager _sdkManager;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUri;
    private readonly List<Scopes> InternalTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.ViewablesRead, Scopes.DataSearch }; // CG: Added DataSearch scope
    private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.ViewablesRead };

    public readonly GraphQLClient GraphQLClient;
    public readonly string CustomPropertyName;

    public APS(string clientId, string clientSecret, string callbackUri, string graphQlEndpoint, string customPropertyName)
    {
        _sdkManager = SdkManagerBuilder.Create().Build();
        _clientId = clientId;
        _clientSecret = clientSecret;
        _callbackUri = callbackUri;

        GraphQLClient = new GraphQLClient(graphQlEndpoint);
        CustomPropertyName = customPropertyName;
    }
}