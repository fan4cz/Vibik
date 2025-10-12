using System.Text.RegularExpressions;
using DotNetEnv;
using Npgsql;

namespace Infrastructure;

public static partial class DataBaseManager
{
    private static string dbConnectionString;

    public static void DataBaseInitialize()
    {
        dbConnectionString =
            $"server={Env.GetString("DB_HOST")};" +
            $" port={Env.GetInt("DB_PORT")};" +
            $" database={Env.GetString("DB_NAME")};" +
            $" username={Env.GetString("DB_USER")};" +
            $" password={Env.GetString("DB_PASSWORD")}";
    }

    public static bool CheckDbConnection()
    {
        var sqlConnection = new NpgsqlConnection(dbConnectionString);
        sqlConnection.Open();
        var dbIsOpen = sqlConnection.State == System.Data.ConnectionState.Open;
        sqlConnection.Close();
        return dbIsOpen;
    }

    public static List<Dictionary<string, object>> SelectCommand(string tableName, params string[] columns)
    {
        if (!IsValidSqlIdentifier(tableName))
            throw new ArgumentException($"Invalid table name: {tableName}");
        foreach (var i in columns)
            if (!IsValidSqlIdentifier(i))
                throw new ArgumentException($"Invalid column name: {i}");
        
        using var sqlConnection = new NpgsqlConnection(dbConnectionString);
        sqlConnection.Open();
        using var command = sqlConnection.CreateCommand();
        command.CommandText = columns.Length == 0
            ? $"SELECT * FROM {tableName}"
            : $"SELECT {string.Join(", ", columns)} FROM {tableName}";

        var reader = command.ExecuteReader();
        var response = new List<Dictionary<string, object>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            response.Add(row);
        }

        return response;
    }


    private static bool IsValidSqlIdentifier(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) &&
               MyRegex().IsMatch(identifier) &&
               !IsSqlKeyword(identifier);
    }

    private static bool IsSqlKeyword(string word)
    {
        var sqlKeywords = new HashSet<string>
        {
            "SELECT", "INSERT", "DELETE", "DROP", "UPDATE", "CREATE",
            "ALTER", "EXEC", "UNION", "WHERE", "FROM"
        };
        return sqlKeywords.Contains(word.ToUpper());
    }

    private class DataBaseResponse(string response, string error, bool isSuccess)
    {
        public string Response { get; private set; } = response;
        public string Error { get; private set; } = error;
        public bool IsSuccess { get; private set; } = isSuccess;
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex MyRegex();
}