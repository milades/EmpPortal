using Microsoft.Data.SqlClient;

namespace EmpPortal.Infrastructure.Configuration;

public static class RuntimeSettingsConfigurationLoader
{
    public static IReadOnlyDictionary<string, string?> Load(
        string connectionString,
        bool required)
    {
        Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            using SqlConnection connection = new(connectionString);
            connection.Open();
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = """
                IF OBJECT_ID(N'[portal].[RuntimeSettings]', N'U') IS NOT NULL
                    SELECT [Key], [Value] FROM [portal].[RuntimeSettings];
                """;
            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (SqlException) when (!required)
        {
            return settings;
        }

        return settings;
    }
}
