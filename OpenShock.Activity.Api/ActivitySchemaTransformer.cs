using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Activity.Api;

/// <summary>
/// Reshapes generated schemas so the OpenAPI document matches how the API actually serializes JSON, keeping
/// the frontend's hey-api client clean and accurate:
/// <list type="bullet">
/// <item><see cref="ulong"/> (Discord snowflakes) → <c>string</c> (see <see cref="UInt64StringConverter"/>).</item>
/// <item>enums → <c>string</c> with their member names (the global <c>JsonStringEnumConverter</c>).</item>
/// <item>collapses the <c>["integer","string"]</c> unions that ASP.NET's web JSON defaults
/// (<c>NumberHandling.AllowReadingFromString</c>) produce back to plain numbers.</item>
/// </list>
/// </summary>
public sealed class ActivitySchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        var nullFlag = (schema.Type ?? default) & JsonSchemaType.Null;

        if (type == typeof(ulong))
        {
            schema.Type = JsonSchemaType.String | nullFlag;
            schema.Format = null;
            schema.Pattern = null;
        }
        else if (type.IsEnum)
        {
            schema.Type = JsonSchemaType.String | nullFlag;
            schema.Format = null;
            schema.Enum = Enum.GetNames(type).Select(name => (JsonNode)name).ToList();
        }
        else if (schema.Type is { } t && t.HasFlag(JsonSchemaType.String) &&
                 (t.HasFlag(JsonSchemaType.Integer) || t.HasFlag(JsonSchemaType.Number)))
        {
            schema.Type = t & ~JsonSchemaType.String;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }
}
