using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class OpcDataRepository: IOpcDataRepository
{
    private readonly string _connectionString;
    
    public OpcDataRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<IEnumerable<OpcData>> GetDataByDeviceId(int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OpcData>> GetDataByDeviceTag(int deviceId, string tag)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OpcData>> GetDataBySourceTime(DateTimeOffset time)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateOpcData(OpcData opcData)
    {
        throw new NotImplementedException();
    }
}