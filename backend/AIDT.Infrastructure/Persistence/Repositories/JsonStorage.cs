using AIDT.Application.Contracts;
using System.Text.Json;

namespace AIDT.Infrastructure.Persistence.Repositories;

internal static class JsonStorage
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);

    public static List<ObjectFieldDto> DeserializeObjectFields(string value)
    {
        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement.EnumerateArray().Select(element =>
        {
            var key = GetString(element, "key")
                ?? GetString(element, "code")
                ?? string.Empty;
            var options = new List<string>();
            if (element.TryGetProperty("options", out var optionsElement))
            {
                if (optionsElement.ValueKind == JsonValueKind.Array)
                {
                    options.AddRange(
                        optionsElement.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(item => !string.IsNullOrWhiteSpace(item))
                            .Select(item => item!));
                }
                else if (optionsElement.ValueKind == JsonValueKind.String)
                {
                    var serializedOptions = optionsElement.GetString();
                    if (!string.IsNullOrWhiteSpace(serializedOptions))
                    {
                        options.AddRange(
                            JsonSerializer.Deserialize<List<string>>(serializedOptions, Options)
                            ?? []);
                    }
                }
            }

            return new ObjectFieldDto(
                key,
                GetString(element, "displayName") ?? key,
                GetString(element, "dataType") ?? "Text",
                GetBoolean(element, "isRequired"),
                GetInt32(element, "maxLength", 100),
                GetString(element, "sensitivity") ?? "Internal",
                GetString(element, "defaultValue"),
                options,
                GetBoolean(element, "showInList", true),
                GetBoolean(element, "filterable"),
                GetInt32(element, "sortOrder", 0),
                GetString(element, "code") ?? key);
        }).ToList();
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBoolean(JsonElement element, string name, bool fallback = false) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;

    private static int GetInt32(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result
            : fallback;
}
