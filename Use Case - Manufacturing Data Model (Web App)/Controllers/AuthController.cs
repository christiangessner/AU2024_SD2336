#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly APS _aps;

    public AuthController(ILogger<AuthController> logger, APS aps)
    {
        _logger = logger;
        _aps = aps;
    }

    public static async Task<Tokens?> PrepareTokens(HttpRequest request, HttpResponse response, APS aps)
    {
        if (!request.Cookies.ContainsKey("internal_token"))
        {
            return null;
        }

        var tokens = new Tokens
        {
            PublicToken = request.Cookies["public_token"],
            InternalToken = request.Cookies["internal_token"],
            RefreshToken = request.Cookies["refresh_token"],
            ExpiresAt = DateTime.Parse(request.Cookies["expires_at"])
        };

        if (tokens.ExpiresAt < DateTime.Now.ToUniversalTime())
        {
            tokens = await aps.RefreshTokens(tokens);
            response.Cookies.Append("public_token", tokens.PublicToken);
            response.Cookies.Append("internal_token", tokens.InternalToken);
            response.Cookies.Append("refresh_token", tokens.RefreshToken);
            response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
        }
        return tokens;
    }

    [HttpGet("login")]
    public ActionResult Login()
    {
        var redirectUri = _aps.GetAuthorizationURL();
        return Redirect(redirectUri);
    }

    [HttpGet("login/project/{project}/item/{item}")]
    public ActionResult Login(string project, string item)
    {
        Response.Cookies.Append("project", project);
        Response.Cookies.Append("item", item);

        var redirectUri = _aps.GetAuthorizationURL();
        return Redirect(redirectUri);
    }

    [HttpGet("logout")]
    public ActionResult Logout()
    {
        Response.Cookies.Delete("public_token");
        Response.Cookies.Delete("internal_token");
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("expires_at");
        return Redirect("/");
    }

    [HttpGet("callback")]
    public async Task<ActionResult> Callback(string code)
    {
        var project = Request.Cookies["project"];
        var item = Request.Cookies["item"];

        Response.Cookies.Delete("project");
        Response.Cookies.Delete("item");

        var tokens = await _aps.GenerateTokens(code);
        Response.Cookies.Append("public_token", tokens.PublicToken);
        Response.Cookies.Append("internal_token", tokens.InternalToken);
        Response.Cookies.Append("refresh_token", tokens.RefreshToken);
        Response.Cookies.Append("expires_at", tokens.ExpiresAt.ToString());
        
        return Redirect("/?project=" + project + "&item=" + item);
    }

    [HttpGet("profile")]
    public async Task<dynamic> GetProfile()
    {
        var tokens = await PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var profile = await _aps.GetUserProfile(tokens);
        return new
        {
            name = profile.Name
        };
    }

    [HttpGet("token")]
    public async Task<dynamic> GetPublicToken()
    {
        var tokens = await PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        return new
        {
            access_token = tokens.PublicToken,
            token_type = "Bearer",
            expires_in = Math.Floor((tokens.ExpiresAt - DateTime.Now.ToUniversalTime()).TotalSeconds)
        };
    }
}