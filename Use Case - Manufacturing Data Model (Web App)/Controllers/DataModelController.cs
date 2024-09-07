using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DataModelController : ControllerBase
{
    private readonly ILogger<DataModelController> _logger;
    private readonly APS _aps;

    public DataModelController(ILogger<DataModelController> logger, APS aps)
    {
        _logger = logger;
        _aps = aps;
    }

    [HttpGet("projects/{project}/items/{item}/hierarchy")]
    public async Task<ActionResult<List<APS.BomRow>>> GetComponentHierarchy(string project, string item)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
            return Unauthorized();

        var hierarchy = await _aps.GetComponentHierarchy(project, item, tokens);
        return hierarchy;
    }

    [HttpGet("componentVersions/{versionId}/async")]
    public async Task<ActionResult<APS.AsyncData>> GetComponentVersionAsyncData(string versionId)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
            return Unauthorized();

        return await _aps.GetAsyncData(versionId, tokens);
    }

    [HttpPost("componentVersions/{versionId}/properties")]
    public async Task<ActionResult<dynamic>> SetProperty(string versionId, [FromBody]UpdatePropertiesRequest body)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
            return Unauthorized();

        return await _aps.SetProperty(versionId, body.DefinitionId, body.Value, tokens);
    }

    [HttpDelete("componentVersions/{versionId}/properties/{definitionId}")]
    public async Task<ActionResult<dynamic>> ClearProperty(string versionId, string definitionId)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
            return Unauthorized();

        return await _aps.ClearProperty(versionId, definitionId, tokens);
    }

    public class UpdatePropertiesRequest
    {
        public required string DefinitionId { get; set; }
        public required object Value { get; set; }
    }

    public class CleanPropertiesRequest
    {
        public required string DefinitionId { get; set; }
    }    
}