using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ErpController : ControllerBase
{
    private readonly ILogger<ErpController> _logger;
    private readonly BC _bc;
    private const string ARROW = "<span>&#8594;</span>";

    public ErpController(ILogger<ErpController> logger, BC bc)
    {
        _logger = logger;
        _bc = bc;
    }

    [HttpPost("items/{number}/compare")]
    public async Task<ActionResult<CompareResult>> CompareBcItem(string number, [FromBody]APS.StaticBomRow bomRow)
    {
        var differences = new List<string>();
        var item = await _bc.GetItemCard(number);
        if (item == null)
            return new CompareResult("add", $"Item '{number}' not found in Business Central!");

        if (item.Description != bomRow.Name)
            differences.Add($"Description: {item.Description} {ARROW} {bomRow.Name}");

        if (item.Description_2 != bomRow.MaterialName)
            differences.Add($"Description_2: {item.Description_2} {ARROW} {bomRow.MaterialName}");

        if (item.Net_Weight != bomRow.Weight)
            differences.Add($"Weight: {item.Net_Weight} {ARROW} {bomRow.Weight}");

        if (item.Unit_Volume != bomRow.Volume)
            differences.Add($"Volume: {item.Unit_Volume} {ARROW} {bomRow.Volume}");

        if (differences.Count == 0)
            return new CompareResult("identical", "Item is identical in Business Central");

        var message = "Item is being updated in Business Central:" + "<br>" + string.Join("<br>", differences);
        return new CompareResult("update", message);
    }

    [HttpPost("items")]
    public async Task<ActionResult<BC.ItemCard>> CreateBcItem([FromBody]APS.StaticBomRow bomRow)
    {
        var bcItemCard = new BC.ItemCard {
            Description = bomRow.Name,
            Description_2 = bomRow.MaterialName,
            Net_Weight = bomRow.Weight,
            Unit_Volume = bomRow.Volume
        };
        
        var item = await _bc.CreateItemCard(bcItemCard);
        await _bc.SetItemPicture(item.No, bomRow.ThumbnailUrl);
        return item;
    }

    [HttpPut("items/{number}")]
    public async Task<ActionResult<BC.ItemCard>> UpdateBcItem(string number, [FromBody]APS.StaticBomRow bomRow)
    {
        if (number != bomRow.ErpNumber)
            return BadRequest();

        var bcItemCard = new BC.ItemCard {
            No = bomRow.ErpNumber,
            Description = bomRow.Name,
            Description_2 = bomRow.MaterialName,
            Net_Weight = bomRow.Weight,
            Unit_Volume = bomRow.Volume
        };
        
        var item = await _bc.UpdateItemCard(number, bcItemCard);
        await _bc.SetItemPicture(item.No, bomRow.ThumbnailUrl);
        return item;
    }


    [HttpPost("boms/{number}/compare")]
    public async Task<ActionResult<CompareResult>> CompareBcBom(string number, [FromBody]APS.StaticBomRow bomRow)
    {
        var differences = new List<string>();

        var bom = await _bc.GetBomHeaderAndLines(number);
        if (bom == null || bom.ProductionBOMsProdBOMLine == null || bom.ProductionBOMsProdBOMLine.Count == 0)
            return new CompareResult("add", $"BOM is being created in Business Central!");

        if (bom.Description != bomRow.Name)
            differences.Add($"Description: {bom.Description}");

        foreach(var row in bomRow.Children)
        {
            var line = bom.ProductionBOMsProdBOMLine.SingleOrDefault(l => l.No.Equals(row.ErpNumber) && l.Position.Equals(row.Position));
            if (line == null)
            {
                differences.Add($"'{row.Name}' is being added at position {row.Position}");
                continue;
            }

            if (line.Quantity_per != row.Quantity)
                differences.Add($"'{row.Name}' Quanity is being updated from {line.Quantity_per} to {row.Quantity}");

            if (line.Description != row.Name)
                differences.Add($"'{row.Name}' Description is being updated from {line.Description} to {row.Name}");
        }

        foreach(var line in bom.ProductionBOMsProdBOMLine)
        {
            var child = bomRow.Children?.SingleOrDefault(c => line.No.Equals(c.ErpNumber) && line.Position.Equals(c.Position));
            if (child == null)
            {
                differences.Add($"'{line.No}' is being removed at position {line.Position}");
                continue;
            }
        }

        if (differences.Count == 0)
            return new CompareResult("identical", "BOM is identical in Business Central");

        var message = "BOM is being updated in Business Central:" + "<br>" + string.Join("<br>", differences);
        return new CompareResult("update", message);
    }

    [HttpPost("boms")]
    public async Task<ActionResult<BC.ProdBomHeader?>> CreateBcBom([FromBody]APS.StaticBomRow bomRow)
    {
        if (bomRow == null || string.IsNullOrEmpty(bomRow.ErpNumber))
            return BadRequest();

        var itemCard = await _bc.GetItemCardBase(bomRow.ErpNumber);
        if (itemCard == null)
            return NotFound();

        var prodBomHeader = await _bc.GetBomHeaderAndLines(bomRow.ErpNumber);
        if (prodBomHeader == null)
        {
            prodBomHeader = new BC.ProdBomHeader
            {
                No = bomRow.ErpNumber,
                Description = bomRow.Name,
            };
            _ = await _bc.CreateBomHeader(prodBomHeader);            
        }
        else
        {
            if (prodBomHeader.ProductionBOMsProdBOMLine != null && prodBomHeader.ProductionBOMsProdBOMLine.Count > 0)
            {
                foreach(var prodBomLine in prodBomHeader.ProductionBOMsProdBOMLine)
                {
                    var child = bomRow.Children?.SingleOrDefault(c => c.ErpNumber == prodBomLine.No && c.Position == prodBomLine.Position.ToString());
                    if (child == null)
                        await _bc.DeleteBomLine(prodBomLine);
                }

            }
        }

        if (prodBomHeader.Description != bomRow.Name)
        {
            prodBomHeader.Description = bomRow.Name;
            _ = await _bc.UpdateBomHeader(prodBomHeader);
        }

        if (bomRow.Children != null && bomRow.Children.Count > 0)
        {
            foreach(var row in bomRow.Children)
            {
                var prodBomLine = new BC.ProdBomLine
                {
                    Production_BOM_No = prodBomHeader.No,
                    No = row.ErpNumber,
                    Position = row.Position,
                    Description = row.Name,
                    Quantity_per = row.Quantity,
                };
                await _bc.CreateBomLine(prodBomLine);
            }            
        }

        await _bc.UpdateItemCardProductionBomNo(itemCard, bomRow.ErpNumber);
        
        return await _bc.GetBomHeaderAndLines(bomRow.ErpNumber);
    }

    [HttpPut("boms/{number}")]
    public async Task<ActionResult<BC.ProdBomHeader?>> UpdateBcBom(string number, [FromBody]APS.StaticBomRow bomRow)
    {
        if (bomRow == null || string.IsNullOrEmpty(bomRow.ErpNumber) || string.IsNullOrEmpty(number) || number != bomRow.ErpNumber)
            return BadRequest();

        var prodBomHeader = await _bc.GetBomHeaderAndLines(number);
        if (prodBomHeader == null)
            return NotFound();

        if (prodBomHeader.Description != bomRow.Name)
        {
            prodBomHeader.Description = bomRow.Name;
            _ = await _bc.UpdateBomHeader(prodBomHeader);
        }
        
        foreach(var prodBomLine in prodBomHeader.ProductionBOMsProdBOMLine)
        {
            var child = bomRow.Children?.SingleOrDefault(c => c.ErpNumber == prodBomLine.No && c.Position == prodBomLine.Position.ToString());
            if (child == null)
                await _bc.DeleteBomLine(prodBomLine);
        }

        if (bomRow.Children != null && bomRow.Children.Count > 0)
        {
            foreach (var row in bomRow.Children)
            {
                var prodBomLine = prodBomHeader.ProductionBOMsProdBOMLine.SingleOrDefault(l => l.No == row.ErpNumber && l.Position.ToString() == row.Position);
                if (prodBomLine == null)
                {
                    prodBomLine = new BC.ProdBomLine
                    {
                        Production_BOM_No = prodBomHeader.No,
                        No = row.ErpNumber,
                        Position = row.Position,
                        Description = row.Name,
                        Quantity_per = row.Quantity,
                    };
                    await _bc.CreateBomLine(prodBomLine);
                } 
                else if (prodBomLine.Quantity_per != row.Quantity || prodBomLine.Description != row.Name)
                {
                    var bomLine = new BC.ProdBomLine
                    {
                        Description = row.Name,
                        Quantity_per = row.Quantity,
                    };
                    await _bc.UpdateBomLine(prodBomLine, bomLine);
                }
            }
        }

        return await _bc.GetBomHeaderAndLines(number);
    }

    #region Serialization Classes
    public class CompareResult
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public CompareResult(string status, string message)
        {
            Status = status;
            Message = message;
        }
    }
    #endregion
}
