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

    public Task<IEnumerable<ErrorLog>> GetErrorLogsBySource(NpgsqlConnection conn, string errorSource, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ErrorLog>> GetErrorLogsByDevice(NpgsqlConnection conn, int deviceId, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ErrorLog>> GetErrorLogsByCode(NpgsqlConnection conn, string errorCode, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateErrorLog(NpgsqlConnection conn, NpgsqlTransaction tx, ErrorLog errorLog)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateErrorLog(NpgsqlConnection conn, NpgsqlTransaction tx, ErrorLog errorLog)
    {
        throw new NotImplementedException();
    }
}