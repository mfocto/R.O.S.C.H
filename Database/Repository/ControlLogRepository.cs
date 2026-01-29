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

    public Task<IEnumerable<ControlLog>> GetLogs(NpgsqlConnection conn, string idType, int Id, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ControlLog>> GetLogs(NpgsqlConnection conn, DateTimeOffset logTime)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, ControlLog controlLog)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, ControlLog controlLog)
    {
        throw new NotImplementedException();
    }
}