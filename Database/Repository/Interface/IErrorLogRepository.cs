using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IErrorLogRepository
{
    Task<IEnumerable<ErrorLog>> GetErrorLogsBySource(NpgsqlConnection conn, string errorSource, DateTimeOffset logTime);
    Task<IEnumerable<ErrorLog>> GetErrorLogsByDevice(NpgsqlConnection conn, int deviceId, DateTimeOffset logTime);
    Task<IEnumerable<ErrorLog>> GetErrorLogsByCode(NpgsqlConnection conn, string errorCode, DateTimeOffset logTime);
    
    Task<int> CreateErrorLog(NpgsqlConnection conn, NpgsqlTransaction tx, ErrorLog errorLog);
}