using Newtonsoft.Json;

public partial class APS
{
    public async Task<List<BomRow>> GetComponentHierarchy(string dmProjectId, string itemId, Tokens tokens)
    {
        var project = await GetProjectId(dmProjectId, tokens);
        var projectId = project.ProjectByDataManagementAPIId.Id;
        var hubId = project.ProjectByDataManagementAPIId.Hub.Id;
        var propertyDefinitionCollections = await GetPropertyDefinitionCollections(hubId, tokens);
        var propertyDefinitions = propertyDefinitionCollections.SelectMany(pdc => pdc.Definitions.Results)?.ToList();
        var propertyDefinition = propertyDefinitions?.Single(pd => pd.Name == CustomPropertyName);

        var tipRootComponentVersion = await GetTipRootComponentVersion(hubId, itemId, CustomPropertyName, tokens);

        void CreateBomStructure(BomRow bomRow, ComponentVersion componentVersion)
        {
            var children = tipRootComponentVersion.AllOccurrences.Results.Where(r => r.ParentComponentVersion.Id == componentVersion.Id).ToList();
            if (children.Count == 0)
            {
                bomRow.Children = [];
                return;
            }

            foreach (var occurrence in children.OrderBy(r => r.ComponentVersion.LastModifiedOn))
            {
                var childComponentVersion = occurrence.ComponentVersion;
                if (bomRow.Children != null && bomRow.Children.Any(r => r.Id == childComponentVersion.Id))
                {
                    var existingBomRow = bomRow.Children.Single(r => r.Id == childComponentVersion.Id);
                    existingBomRow.Quantity++;
                }
                else
                {
                    var childBomRow = CreateBomRow(childComponentVersion, bomRow);
                    CreateBomStructure(childBomRow, childComponentVersion);
                    bomRow.Children?.Add(childBomRow);
                }
            }
        }

        BomRow CreateBomRow(ComponentVersion componentVersion, BomRow? parentBomRow = null)
        {
            return new BomRow
            {
                Parent = parentBomRow,
                Id = componentVersion.Id,
                Name = componentVersion.Name,
                PartNumber = componentVersion.PartNumber,
                PartDescription = componentVersion.PartDescription,
                MaterialName = componentVersion.MaterialName,
                ErpNumber = componentVersion.CustomProperties.Results.FirstOrDefault(
                    p => p.Name == CustomPropertyName)?.Value.ToString() ?? string.Empty,
                ErpNumberDefinitionId = propertyDefinition.Id,
                ThumbnailUrl = string.Empty,

            };
        }

        var tipBomRow = CreateBomRow(tipRootComponentVersion);
        CreateBomStructure(tipBomRow, tipRootComponentVersion);

        var result = new List<BomRow> { tipBomRow };
        return result;
    }

    public async Task<AsyncData> GetAsyncData(string componentVersionId, Tokens tokens)
    {
        var counter = 5;
        global::AsyncData.ComponentVersion? componentVersion = null;
        var success = false;
        while (!success || counter == 0)
        {
            counter--;
            componentVersion = await GetComponentVersionAsyncData(componentVersionId, tokens);
            success = componentVersion.Thumbnail.Status == "SUCCESS" && componentVersion.PhysicalProperties.Status == "COMPLETED";
            if (!success)
                await Task.Delay(1000);
        }

        if (componentVersion == null)
            throw new Exception("Failed to retrieve Async Data");

        var result = new AsyncData
        {
            ThumbnailUrl = componentVersion.Thumbnail.SignedUrl,
            Weight = Convert.ToDecimal(componentVersion.PhysicalProperties.Mass.Value),
            Volume = Convert.ToDecimal(componentVersion.PhysicalProperties.Volume.Value)
        };

        return result;
    }

    #region Serialization Classes
    public class StaticBomRow : AsyncData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string ErpNumber { get; set; } = string.Empty;
        public string ErpNumberDefinitionId { get; set; } = string.Empty;
        public virtual List<StaticBomRow> Children { get; set; } = [];
        public virtual string Position { get; set; } = string.Empty;
        public int Quantity = 1;
        public string ItemStatus { get; set; } = string.Empty;
        public string ItemTooltip { get; set; } = string.Empty;
        public string BomStatus { get; set; } = string.Empty;
        public string BomTooltip { get; set; } = string.Empty;
    }

    public class AsyncData
    {
        public string ThumbnailUrl { get; set; } = string.Empty;

        private decimal _weight = 0;
        public decimal Weight {
            get => Math.Round(_weight, 5);
            set => _weight = value;
        }

        private decimal _volume = 0;
        public decimal Volume {
            get => Math.Round(_volume, 5); 
            set => _volume = value;
        }
    }

    public class BomRow : StaticBomRow
    {
        [JsonIgnore]
        public required BomRow? Parent { get; set; }
        public override List<StaticBomRow> Children { get; set; } = [];
        public string AbsolutePosition => Parent == null ? "1" : $"{Parent.AbsolutePosition}.{Parent.Children?.IndexOf(this) + 1}";
        public override string Position => Parent == null ? "0" : $"{Parent.Children?.IndexOf(this) + 1}";
    }    
    #endregion
}
