using Newtonsoft.Json;
using RestSharp;

public partial class BC
{
    public async Task<ProdBomHeaderBase> GetBomHeaderMin(string number)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMs('{number}')?$select=No");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ProdBomHeaderBase>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to retrieve Production BOM Header");

        return response.Data;
    }

    public async Task<ProdBomHeader?> GetBomHeaderAndLines(string number)
    {
        var selectHeader = "No,Description,Unit_of_Measure_Code";
        var selectLines = "Production_BOM_No,Version_Code,Line_No,No,Position,Description,Quantity_per";
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMs('{number}')?$expand=ProductionBOMsProdBOMLine($select={selectLines})&$select={selectHeader}");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ProdBomHeader>(request);
        if (!response.IsSuccessful)
            return null;

        if (response.Data == null)
            throw new Exception("Failed to retrieve Production BOM Header and Lines");

        return response.Data;
    }

    public async Task<ProdBomHeader> CreateBomHeader(ProdBomHeader prodBomHeader)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMs", Method.Post);
        var json = JsonConvert.SerializeObject(
            new
            {
                prodBomHeader.No,
                prodBomHeader.Description,
                Unit_of_Measure_Code = "PCS",
                Status = "New"
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ProdBomHeader>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to create Production BOM Header");

        return response.Data;
    }

    public async Task<ProdBomHeader> UpdateBomHeader(ProdBomHeader prodBomHeader)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMs('{prodBomHeader.No}')", Method.Patch);
        request.AddHeader("If-Match", prodBomHeader.odataetag);
        var json = JsonConvert.SerializeObject(
            new
            {
                prodBomHeader.No,
                prodBomHeader.Description,
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ProdBomHeader>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to update Production BOM Header");

        return response.Data;
    }

    #region Serialization Classes
    public class ProdBomHeaderBase
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; } = string.Empty;
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; } = string.Empty;
        public string No { get; set; } = string.Empty;
    }

    public class ProdBomHeader : ProdBomHeaderBase
    {
        public string Description { get; set; } = string.Empty;
        public string Unit_of_Measure_Code { get; set; } = string.Empty;
        public List<ProdBomLine> ProductionBOMsProdBOMLine { get; set; } = new List<ProdBomLine>();
    }
    #endregion
}
