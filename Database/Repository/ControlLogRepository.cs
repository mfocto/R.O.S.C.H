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
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<IEnumerable<ControlLog>> GetLogs(string idType, int Id, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ControlLog>> GetLogs(DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateAsync(ControlLog controlLog)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateAsync(ControlLog controlLog)
    {
        throw new NotImplementedException();
    }
}