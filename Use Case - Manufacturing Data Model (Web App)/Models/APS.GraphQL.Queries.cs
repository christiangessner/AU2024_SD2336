public partial class APS
{
    public async Task<Project.Data> GetProjectId(string projectId, Tokens tokens)
    {
        var query = @"query GetProject($dataManagementAPIProjectId: ID!) {
            projectByDataManagementAPIId(dataManagementAPIProjectId: $dataManagementAPIProjectId) {
                id
                name
                alternativeIdentifiers {
                    dataManagementAPIProjectId
                }
                hub {
                    id
                    name
                }
            }
        }";
        var variables = new
        {
            dataManagementAPIProjectId = projectId
        };

        var root = await GraphQLClient.Query<Project.Root>(query, variables, tokens.InternalToken);
        if (root == null)
            throw new Exception("Failed to get project by data management API");

        return root.Data;
    }

    public async Task<List<Collections.PropertyDefinitionCollection>> GetPropertyDefinitionCollections(string hubId, Tokens tokens)
    {
        var query = @"query GetPropertyDefinitionCollections($hubId:ID!) {
            hub(hubId: $hubId) {
                propertyDefinitionCollections {
                    results {
                        id
                        name
                        definitions {
                            results {
                                name
                                id
                            }
                        }
                    }
                }
            }
        }";
        var variables = new
        {
            hubId
        };

        var root = await GraphQLClient.Query<Collections.Root>(query, variables, tokens.InternalToken);
        if (root == null)
            throw new Exception("Failed to get project by data management API");

        return root.Data.Hub.PropertyDefinitionCollections.Results;
    }

    public async Task<TipRoot.ComponentVersion> GetTipRootComponentVersion(string hubId, string itemId, string propName, Tokens tokens)
    {
        var query = @"query GetTipRootComponentVersion($hubId: ID!, $itemId: ID!, $propName: [String!]!) {
            item(hubId: $hubId, itemId: $itemId) {
                ... on DesignItem {
                    tipRootComponentVersion {
                        ...commonProps
                        allOccurrences(pagination: {limit: 50}) {
                            pagination {
                                pageSize
                                cursor
                            }
                            results {
                                id
                                parentComponentVersion {
                                    id
                                }
                                componentVersion {
                                    ...commonProps
                                }
                            }
                        }
                    }
                }
            }
        }

        fragment commonProps on ComponentVersion {
            id
            lastModifiedOn
            name
            partNumber
            partDescription
            materialName
            customProperties(filter: {names: $propName}) {
            results {
                    name
                    value
                }
            }
        }";
        var variables = new
        {
            hubId,
            itemId,
            propName
        };

        var root = await GraphQLClient.Query<TipRoot.Root>(query, variables, tokens.InternalToken);
        if (root == null)
            throw new Exception("Failed to retrieve Component Hierarchy");

        var cursor = root.Data.Item.TipRootComponentVersion.AllOccurrences.Pagination.Cursor;
        while (cursor != null)
        {
            var occurrences = await GetAllOccurrencesFromCursor(
                root.Data.Item.TipRootComponentVersion.Id,
                propName,
                cursor,
                tokens);

            if (occurrences == null)
                throw new Exception("Failed to retrieve all occurrences");

            root.Data.Item.TipRootComponentVersion.AllOccurrences.Results.AddRange(occurrences.Results);
            cursor = occurrences.Pagination.Cursor;
        }

        return root.Data.Item.TipRootComponentVersion;
    }

    public async Task<Occurrences> GetAllOccurrencesFromCursor(string tipRootComponentVersionId, string propName, string cursor, Tokens tokens)
    {
        var query = @"query GetAllOccurrences($componentVersionId: ID!, $propName: [String!]!, $cursor: String) {
            componentVersion(componentVersionId: $componentVersionId) {
                allOccurrences(pagination: {cursor: $cursor, limit: 50}) {
                    pagination {
                        pageSize
                        cursor
                    }
                    results {
                        id
                        parentComponentVersion {
                            id
                        }
                        componentVersion {
                            id
                            lastModifiedOn
                            name
                            partNumber
                            partDescription
                            materialName
                            customProperties(filter:{names: $propName}) {
                                results {
                                    name
                                    value
                                }
                            }
                        }
                    }
                }
            }
        }";
        var variables = new
        {
            componentVersionId = tipRootComponentVersionId,
            propName,
            cursor
        };

        var root = await GraphQLClient.Query<AllOccurrences.Root>(query, variables, tokens.InternalToken);
        if (root == null)
            throw new Exception("Failed to retrieve Model Hierarchy");

        return root.Data.ComponentVersion.AllOccurrences;
    }

    public async Task<global::AsyncData.ComponentVersion> GetComponentVersionAsyncData(string componentVersionId, Tokens tokens)
    {
        var query = @"query GetComponentVersionAsyncData($componentVersionId: ID!) {
            componentVersion(componentVersionId: $componentVersionId) {
                id
                thumbnail {
                    status
                    signedUrl
                }
                physicalProperties {
                    status
                    volume {
                        value
                    }
                    mass {
                        value
                    }
                }
            }
        }";
        var variables = new
        {
            componentVersionId
        };

        var root = await GraphQLClient.Query<global::AsyncData.Root>(query, variables, tokens.InternalToken);
        if (root == null)
            throw new Exception("Failed to retrieve Async Data");

        return root.Data.ComponentVersion;
    }
}