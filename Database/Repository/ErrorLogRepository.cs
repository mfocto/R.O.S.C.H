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
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<IEnumerable<ErrorLog>> GetErrorLogsBySource(string errorSource, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ErrorLog>> GetErrorLogsByDevice(int deviceId, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ErrorLog>> GetErrorLogsByCode(string errorCode, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateErrorLog(ErrorLog errorLog)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateErrorLog(ErrorLog errorLog)
    {
        throw new NotImplementedException();
    }
}