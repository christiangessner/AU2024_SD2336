using Newtonsoft.Json;
using RestSharp;

public partial class BC
{
    public async Task<ItemCardBase> GetItemCardBase(string number)
    {
        var request = GetRestRequest($"/Company('{_company}')/ItemCards('{number}')?$select=No");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ItemCardBase>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to retrieve Item Card");

        return response.Data;
    }

    public async Task<ItemCard?> GetItemCard(string number)
    {
        var select = "No,Description,Description_2,Net_Weight,Unit_Volume,Unit_Price,Inventory,Blocked";
        var request = GetRestRequest($"/Company('{_company}')/ItemCards('{number}')?$select={select}");
        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ItemCard>(request);
        if (!response.IsSuccessful)
            return null;

        if (response.Data == null)
            throw new Exception("Failed to retrieve Item Card");

        return response.Data;
    }

    private async Task<ItemItemspicture?> GetItemItemspicture(string number)
    {
        var expand = "Itemspicture($select=id,pictureContent)";
        var select = "id,number";
        var request = GetRestRequest(
            $"/Company('{_company}')/Items?$filter=number eq '{number}'&$expand={expand}&$select={select}");

        request.AddHeader("Accept", "application/json");

        var response = await GetRestClient().ExecuteAsync<ODataResponse<ItemItemspicture>>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to retrieve Item Picture");

        return response.Data.Value.FirstOrDefault();
    }

    public async Task SetItemPicture(string number, string url)
    {
        var thumbnail = await GetResourceAsByteArray(url);
        if (thumbnail == null || thumbnail.Length <= 0)
            return;

        var itemItemspicture = await GetItemItemspicture(number);
        if (itemItemspicture == null)
            throw new Exception("Failed to retrieve Item Picture");

        var request = GetRestRequest(
            $"/Company('{_company}')/Items({itemItemspicture.id})/Itemspicture/pictureContent", Method.Patch);

        request.AddHeader("If-Match", itemItemspicture.Itemspicture.odataetag);
        request.AddParameter("application/octet-stream", thumbnail, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);
    }

    public async Task<ItemCard> CreateItemCard(ItemCard itemCard)
    {
        var request = GetRestRequest(
            $"/Company('{_company}')/ItemCards", Method.Post);

        var json = JsonConvert.SerializeObject(
            new
            {
                itemCard.Description,
                itemCard.Description_2,
                itemCard.Net_Weight,
                itemCard.Unit_Volume,
                Blocked = false,
                Type = "Inventory",
                Base_Unit_of_Measure = "PCS",
                Inventory_Posting_Group = "FINISHED",
                Item_Category_Code = "PARTS",
                Gen_Prod_Posting_Group = "MANUFACT",
                Replenishment_System = "Purchase"
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ItemCard>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to create Item Card");

        return response.Data;
    }

    public async Task<ItemCard> UpdateItemCard(string number, ItemCard itemCard)
    {
        var itemCardMin = await GetItemCardBase(number);

        var request = GetRestRequest(
            $"/Company('{_company}')/ItemCards('{itemCard.No}')", Method.Patch);

        request.AddHeader("If-Match", itemCardMin.odataetag);
        var json = JsonConvert.SerializeObject(
            new
            {
                itemCard.No,
                itemCard.Description,
                itemCard.Description_2,
                itemCard.Net_Weight,
                itemCard.Unit_Volume,
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ItemCard>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to update Item Card");

        return response.Data;
    }

    public async Task<ItemCard> UpdateItemCardProductionBomNo(ItemCardBase itemCard, string number)
    {
        var request = GetRestRequest($"/Company('{_company}')/ItemCards('{itemCard.No}')", Method.Patch);
        request.AddHeader("If-Match", itemCard.odataetag);
        var json = JsonConvert.SerializeObject(
            new
            {
                Production_BOM_No = number
            },
            Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        request.AddParameter("application/json", json, ParameterType.RequestBody);

        var response = await GetRestClient().ExecuteAsync<ItemCard>(request);
        if (!response.IsSuccessful)
            throw new Exception(response.Content);

        if (response.Data == null)
            throw new Exception("Failed to update Production BOM No. on Item Card");

        return response.Data;
    }

    #region Serialization Classes
    internal class ODataResponse<T>
    {
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; } = string.Empty;

        [JsonProperty("value")]
        public List<T> Value { get; set; } = new List<T>();
    }

    public class ItemItemspicture
    {
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public Itemspicture Itemspicture { get; set; } = new Itemspicture();
    }

    public class Itemspicture
    {
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        [JsonProperty("pictureContent@odata.mediaEditLink")]
        public string pictureContentodatamediaEditLink { get; set; } = string.Empty;
        [JsonProperty("pictureContent@odata.mediaReadLink")]
        public string pictureContentodatamediaReadLink { get; set; } = string.Empty;
    }

    public class ItemCardBase
    {
        [JsonProperty("@odata.etag")]
        public string odataetag { get; set; } = string.Empty;
        public string No { get; set; } = string.Empty;
    }

    public class ItemCard : ItemCardBase
    {
        public string Description { get; set; } = string.Empty;
        public string Description_2 { get; set; } = string.Empty;
        public decimal Net_Weight { get; set; }
        public decimal Unit_Volume { get; set; }
        public decimal Unit_Price { get; set; }
        public double Inventory { get; set; }
        public bool Blocked { get; set; }
    }
    #endregion
}
