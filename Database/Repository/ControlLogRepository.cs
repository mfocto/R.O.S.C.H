using Dapper;
using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class ControlLogRepository: IControlLogRepository
{
    private readonly string _connectionString;
    
    public ControlLogRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }

    public async Task<IEnumerable<ControlLog>> GetLogs(NpgsqlConnection conn, string idType, int Id, DateTimeOffset logTime)
    {
        string columnName = idType.ToLower() switch
        {
            "user" => "user_id",
            "device" => "device_id",
            _ => throw new ArgumentException($"Invalid idType: {idType}")
        };
        
        string sql = $"""
                     SELECT log_id as LogId,
                            user_id as UserId,
                            device_id as DeviceId,
                            control_type as ControlType,
                            tag_name as TagName,
                            old_value as OldValue,
                            new_value as NewValue,
                            created_at as CreatedAt
                     FROM   "control_log"
                     WHERE  {columnName} = @Id
                     AND    created_at >= @LogTime
                     ORDER BY created_at DESC
                     """;
        
        return await conn.QueryAsync<ControlLog>(sql, new{ Id = Id, LogTime = logTime });
    }

    public async Task<IEnumerable<ControlLog>> GetLogs(NpgsqlConnection conn, DateTimeOffset logTime)
    {
        string sql = """
                     SELECT log_id as LogId,
                            user_id as UserId,
                            device_id as DeviceId,
                            control_type as ControlType,
                            tag_name as TagName,
                            old_value as OldValue,
                            new_value as NewValue,
                            created_at as CreatedAt
                     FROM    "control_log"
                     WHERE  created_at >= @LogTime
                     """;
        
        return await conn.QueryAsync<ControlLog>(sql, new { LogTime = logTime });
    }

    public async Task<int> CreateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, ControlLog controlLog)
    {
        string sql = """
                     INSERT INTO "control_log" (user_id, device_id, control_type, tag_name, new_value) 
                     VALUES (@UserId, @DeviceId, @ControlType, @TagName, @NewValue)
                     """;
        
        return await conn.ExecuteAsync(sql, controlLog);
    }
    
}