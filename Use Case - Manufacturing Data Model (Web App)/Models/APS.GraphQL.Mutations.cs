public partial class APS
{
    public async Task<dynamic> SetProperty(string componentVersionId, string propertyDefinitionId, object value, Tokens tokens)
    {
        var query = @"mutation SetDynamicProperty($input: SetPropertiesInput!) {
            setProperties(input: $input) {
                targetId
                properties {
                    value
                }
            }
        }";

        var variables = new
        {
            input = new
            {
                targetId = componentVersionId,
                propertyInputs = new[]
                {
                    new
                    {
                        propertyDefinitionId,
                        value
                    }
                }
            }
        };

        var result = await GraphQLClient.Query<dynamic>(query, variables, tokens.InternalToken);
        if (result == null)
            throw new Exception("Failed to update property");

        return result;
    }

    public async Task<dynamic> ClearProperty(string componentVersionId, string propertyDefinitionId, Tokens tokens)
    {
        var query = @"mutation SetDynamicProperty($input: SetPropertiesInput!) {
            setProperties(input: $input) {
                targetId
                properties {
                    value
                }
            }
        }";

        var variables = new
        {
            input = new
            {
                targetId = componentVersionId,
                propertyInputs = new[]
                {
                    new
                    {
                        propertyDefinitionId,
                        shouldClear = true
                    }
                }
            }
        };

        var result = await GraphQLClient.Query<dynamic>(query, variables, tokens.InternalToken);
        if (result == null)
            throw new Exception("Failed to clear property");

        return result;
    }
}