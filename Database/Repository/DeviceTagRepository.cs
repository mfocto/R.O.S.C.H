using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class DeviceTagRepository: IDeviceTagRepository
{
    private readonly string _connectionString;
    
    public DeviceTagRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }

    public Task<IEnumerable<DeviceTag>> GetDataByDeviceId(NpgsqlConnection conn, int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByChannel(NpgsqlConnection conn, string channel)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByDeviceName(NpgsqlConnection conn, string channel, string deviceName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataActive(NpgsqlConnection conn, string channel, string deviceName, bool isActive)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByAccessType(NpgsqlConnection conn, string accessType)
    {
        throw new NotImplementedException();
    }
}