using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmpPortal.Application.Forms.Schema;

public static class FormSchemaSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(FormSchemaDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerializer.Serialize(definition, SerializerOptions);
    }

    public static FormSchemaDefinition Deserialize(string definitionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        return JsonSerializer.Deserialize<FormSchemaDefinition>(definitionJson, SerializerOptions) ??
            throw new JsonException("The form schema is empty.");
    }

    public static string ComputeHash(string definitionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionJson);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(definitionJson));
        return Convert.ToHexString(hash);
    }
}
