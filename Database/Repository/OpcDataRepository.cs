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

    public Task<IEnumerable<OpcData>> GetDataByDeviceId(NpgsqlConnection conn, int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OpcData>> GetDataByDeviceTag(NpgsqlConnection conn, int deviceId, string tag)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OpcData>> GetDataBySourceTime(NpgsqlConnection conn, DateTimeOffset time)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateOpcData(NpgsqlConnection conn, NpgsqlTransaction tx, OpcData opcData)
    {
        throw new NotImplementedException();
    }
}