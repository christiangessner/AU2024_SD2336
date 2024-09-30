using System.Text.Json.Serialization;

namespace Project
{
    public class Root
    {
        [JsonPropertyName("data")]
        public required Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("projectByDataManagementAPIId")]
        public required ProjectByDataManagementAPIId ProjectByDataManagementAPIId { get; set; }
    }

    public class ProjectByDataManagementAPIId
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("alternativeIdentifiers")]
        public required AlternativeIdentifiers AlternativeIdentifiers { get; set; }

        [JsonPropertyName("hub")]
        public required Hub Hub { get; set; }
    }

    public class AlternativeIdentifiers
    {
        [JsonPropertyName("dataManagementAPIProjectId")]
        public required string dataManagementAPIProjectId { get; set; }
    }

    public class Hub
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("propertyDefinitionCollections")]
        public required PropertyDefinitionCollections PropertyDefinitionCollections { get; set; }
    }


    public class PropertyDefinitionCollections
    {
        [JsonPropertyName("results")]
        public required List<PropertyDefinitionCollection> Results { get; set; }
    }

    public class PropertyDefinitionCollection
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("definitions")]
        public required Definitions Definitions { get; set; }
    }

    public class Definitions
    {
        [JsonPropertyName("results")]
        public required List<Definition> Results { get; set; }
    }

    public class Definition
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }
    }
}

namespace TipRoot
{
    public class Root
    {
        [JsonPropertyName("data")]
        public required Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("item")]
        public required Item Item { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("tipRootComponentVersion")]
        public required TipRootComponentVersion TipRootComponentVersion { get; set; }
    }

    public class TipRootComponentVersion : ChildComponentVersion
    {
        [JsonPropertyName("allOccurrences")]
        public required Occurrences AllOccurrences { get; set; }
    }
}

namespace AllOccurrences
{
    public class Root
    {
        [JsonPropertyName("data")]
        public required Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("componentVersion")]
        public required ComponentVersion ComponentVersion { get; set; }
    }

    public class ComponentVersion
    {
        [JsonPropertyName("allOccurrences")]
        public required Occurrences AllOccurrences { get; set; }
    }
}

namespace AsyncData
{
    public class Root
    {
        [JsonPropertyName("data")]
        public required Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("componentVersion")]
        public required ComponentVersion ComponentVersion { get; set; }
    }

    public class ComponentVersion
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("thumbnail")]
        public required Thumbnail Thumbnail { get; set; }

        [JsonPropertyName("physicalProperties")]
        public required PhysicalProperties PhysicalProperties { get; set; }
    }
}

public class Occurrences
{
    [JsonPropertyName("results")]
    public required List<Occurrence> Results { get; set; }

    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; set; }
}

public class Occurrence
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("childComponentVersion")]
    public required ChildComponentVersion ChildComponentVersion { get; set; }

    [JsonPropertyName("parentComponentVersion")]
    public required ParentComponentVersion ParentComponentVersion { get; set; }
}

public class ParentComponentVersion
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}

public class ChildComponentVersion
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("lastModifiedOn")]
    public required string LastModifiedOn { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("partNumber")]
    public required string PartNumber { get; set; }

    [JsonPropertyName("partDescription")]
    public required string PartDescription { get; set; }

    [JsonPropertyName("materialName")]
    public required string MaterialName { get; set; }

    [JsonPropertyName("customProperties")]
    public required CustomProperties CustomProperties { get; set; }
}

public class PhysicalProperties
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("volume")]
    public required PhysicalProperty Volume { get; set; }

    [JsonPropertyName("mass")]
    public required PhysicalProperty Mass { get; set; }
}

public class PhysicalProperty
{
    [JsonPropertyName("value")]
    public required object Value { get; set; }
}

public class CustomProperties
{
    [JsonPropertyName("results")]
    public required List<CustomProperty> Results { get; set; }
}

public class CustomProperty
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required object Value { get; set; }
}

public class Thumbnail
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("signedUrl")]
    public required string SignedUrl { get; set; }
}

public class Pagination
{
    [JsonPropertyName("pageSize")]
    public required int PageSize { get; set; }

    [JsonPropertyName("cursor")]
    public required string Cursor { get; set; }
}