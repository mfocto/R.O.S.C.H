using Dapper;
using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class ErrorLogRepository: IErrorLogRepository
{
    private readonly string _connectionString;
    public ErrorLogRepository (IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }

    public async Task<IEnumerable<ErrorLog>> GetErrorLogsBySource(NpgsqlConnection conn, string errorSource, DateTimeOffset logTime)
    {
        string sql = """
                     SELECT error_id AS ErrorId,
                     error_code AS ErrorCode,
                     error_source AS ErrorSource,
                     error_msg AS ErrorMsg,
                     user_id AS UserId,
                     device_id AS DeviceId,
                     created_at AS CreatedAt
                     FROM   "error_log"
                     WHERE  error_source = @ErrorSource
                     AND    created_at >= @CreatedAt
                     """;
        
        return await conn.QueryAsync<ErrorLog>(sql, new { ErrorSource = errorSource,  CreatedAt = logTime.ToUnixTimeSeconds() });
    }

    public async Task<IEnumerable<ErrorLog>> GetErrorLogsByDevice(NpgsqlConnection conn, int deviceId, DateTimeOffset logTime)
    {
        string sql = """
                     SELECT error_id AS ErrorId,
                     error_code AS ErrorCode,
                     error_source AS ErrorSource,
                     error_msg AS ErrorMsg,
                     user_id AS UserId,
                     device_id AS DeviceId,
                     created_at AS CreatedAt
                     FROM   "error_log"
                     WHERE  device_id = @DeviceId
                     AND    created_at >= @CreatedAt
                     """;
        
        return await conn.QueryAsync<ErrorLog>(sql, new { DeviceId = deviceId,  CreatedAt = logTime.ToUnixTimeSeconds() });
    }

    public async Task<IEnumerable<ErrorLog>> GetErrorLogsByCode(NpgsqlConnection conn, string errorCode, DateTimeOffset logTime)
    {
        string sql = """
                     SELECT error_id AS ErrorId,
                     error_code AS ErrorCode,
                     error_source AS ErrorSource,
                     error_msg AS ErrorMsg,
                     user_id AS UserId,
                     device_id AS DeviceId,
                     created_at AS CreatedAt
                     FROM   "error_log"
                     WHERE  error_code = @ErrorCode
                     AND    created_at >= @CreatedAt
                     """;

        return await conn.QueryAsync<ErrorLog>(sql, new { ErrorCode = errorCode,  CreatedAt = logTime.ToUnixTimeSeconds() });
    }

    public async Task<int> CreateErrorLog(NpgsqlConnection conn, NpgsqlTransaction tx, ErrorLog errorLog)
    {
        string sql = """
                     INSERT INTO "error_log" (error_code, error_source, error_msg, stack_trace, user_id, device_id)
                     VALUES (@ErrorCode, @ErrorSource, @ErrorMsg, @StackTrace, @UserId, @DeviceId)
                     """;
        
        return await conn.ExecuteAsync(sql, errorLog);
    }

}