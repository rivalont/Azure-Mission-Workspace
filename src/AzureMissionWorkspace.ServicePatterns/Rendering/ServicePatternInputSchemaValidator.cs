using System.Text.Json;
using System.Text.Json.Nodes;
using AzureMissionWorkspace.Application.Abstractions.Services;
using Json.Schema;

namespace AzureMissionWorkspace.ServicePatterns.Rendering;

/// <summary>
/// Validates requestor-supplied parameter values against a service pattern's JSON Schema
/// (<c>input-schema.json</c>), in addition to the lighter-weight required-input check performed by
/// the Application layer.
/// </summary>
public sealed class ServicePatternInputSchemaValidator : IInputSchemaValidator
{
    public InputSchemaValidationResult Validate(string inputSchemaJson, IReadOnlyDictionary<string, string> parameterValues)
    {
        var schema = JsonSchema.FromText(inputSchemaJson);

        var instance = new JsonObject();
        foreach (var (name, value) in parameterValues)
        {
            instance[name] = JsonNode.Parse(TryAsJsonLiteral(value));
        }

        var result = schema.Evaluate(instance.Deserialize<JsonElement>(), new EvaluationOptions { OutputFormat = OutputFormat.List });

        if (result.IsValid)
        {
            return new InputSchemaValidationResult(true, []);
        }

        var errors = (result.Details ?? [])
            .Where(d => !d.IsValid && d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))
            .ToArray();

        return new InputSchemaValidationResult(false, errors.Length > 0 ? errors : ["Input schema validation failed."]);
    }

private static string TryAsJsonLiteral(string value)
{
    // Parameter values are collected as strings. If the value is already valid JSON (number, bool,
    // array, object, or quoted string), keep it. Otherwise, encode it as a JSON string literal.
    try
    {
        JsonDocument.Parse(value);
        return value;
    }
    catch (JsonException)
    {
        return JsonSerializer.Serialize(value);
    }
}
}