using Newtonsoft.Json;
using RestSharp;

public partial class BC
{
    public async Task<ProdBomLineBase> GetBomLineMin(string parentNumber, string childNumber, string position)
    {
        var filter = $"Production_BOM_No eq '{parentNumber}' and No eq '{childNumber}' and Position eq '{position}'";
        var select = "Production_BOM_No,Version_Code,Line_No";
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMLines?$filter={filter}&$select={select}");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ProdBomLineBase>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to retrieve Production BOM Line");

        return response.Data;
    }

    public async Task<ProdBomLine?> GetBomLine(string parentNumber, string childNumber, string position)
    {
        var filter = $"Production_BOM_No eq '{parentNumber}' and No eq '{childNumber}' and Position eq '{position}'";
        var select = "Production_BOM_No,Version_Code,Line_No,No,Position,Description,Quantity_per";
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMLines?$filter={filter}&$select={select}");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ProdBomLine>(request);
        if (!response.IsSuccessful)
            return null;

        if (response.Data == null)
            throw new Exception("Failed to retrieve Production BOM Line");

        return response.Data;
    }

    public async Task<ProdBomLine> CreateBomLine(ProdBomLine bomLine)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMLines", Method.Post);
        var json = JsonConvert.SerializeObject(
            new
            {
                bomLine.Production_BOM_No,
                bomLine.No,
                bomLine.Position,
                bomLine.Description,
                bomLine.Quantity_per,
                Type = "Item",
                Version_Code = "",
                Unit_of_Measure_Code = "PCS"
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ProdBomLine>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to create Production BOM Line");

        return response.Data;
    }

    public async Task<ProdBomLine> UpdateBomLine(ProdBomLineBase existingBomLine, ProdBomLine bomLine)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMLines('{existingBomLine.Production_BOM_No}','{existingBomLine.Version_Code}',{existingBomLine.Line_No})", Method.Patch);
        request.AddHeader("If-Match", existingBomLine.odataetag);
        var json = JsonConvert.SerializeObject(
            new
            {
                bomLine.Description,
                bomLine.Quantity_per,
                Version_Code = "",
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ProdBomLine>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to update Production BOM Line");

        return response.Data;
    }

    public async Task DeleteBomLine(ProdBomLineBase bomLine)
    {
        var request = GetRestRequest($"/Company('{_company}')/ProductionBOMLines('{bomLine.Production_BOM_No}','{bomLine.Version_Code}',{bomLine.Line_No})", Method.Delete);
        request.AddHeader("If-Match", bomLine.odataetag);

        var response = await GetRestClient().ExecuteAsync(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);
    }

    #region Serialization Classes
    public class ProdBomLineBase
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; } = string.Empty;
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; } = string.Empty;
        public string Production_BOM_No { get; set; } = string.Empty;
        public string Version_Code { get; set; } = string.Empty;
        public int Line_No { get; set; }
    }

    public class ProdBomLine : ProdBomLineBase
    {
        public string No { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity_per { get; set; }
    }
    #endregion
}
