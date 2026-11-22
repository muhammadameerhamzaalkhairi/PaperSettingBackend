using System.Data;
using BIITPaperSettingAPI.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BIITPaperSettingAPI.Services;

public class SetupProcedures(BiitDbContext db)
{
    private SqlConnection Connect() => new SqlConnection(db.Database.GetConnectionString());

    // Procedure ka naam sirf controller deta hai. User ki tamam values parameters hain.
    private static SqlCommand Command(SqlConnection connection, string procedure, int actorId)
    {
        var command = new SqlCommand(procedure, connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@ActorId", SqlDbType.Int).Value = actorId;
        return command;
    }

    public async Task<int?> WriteAsync(string procedure, int actorId, SqlParameter[] parameters, CancellationToken cancellation)
    {
        await using var connection = Connect();
        using var command = Command(connection, procedure, actorId);
        command.Parameters.AddRange(parameters);
        await connection.OpenAsync(cancellation);
        var value = await command.ExecuteScalarAsync(cancellation);
        if (value is int id)
            return id;
        return null;
    }

    public async Task<Dictionary<string, List<Dictionary<string, object?>>>> ReadAsync(
        string procedure, int actorId, string[] names, CancellationToken cancellation)
    {
        await using var connection = Connect();
        using var command = Command(connection, procedure, actorId);
        await connection.OpenAsync(cancellation);
        using var reader = await command.ExecuteReaderAsync(cancellation);
        var result = new Dictionary<string, List<Dictionary<string, object?>>>();
        foreach (string name in names)
        {
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(cancellation))
            {
                var row = new Dictionary<string, object?>();
                for (int column = 0; column < reader.FieldCount; column++)
                {
                    row[reader.GetName(column)] = reader.IsDBNull(column) ? null : reader.GetValue(column);
                }
                rows.Add(row);
            }
            result.Add(name, rows);
            await reader.NextResultAsync(cancellation);
        }
        return result;
    }
}
