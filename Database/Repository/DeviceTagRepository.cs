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
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<IEnumerable<DeviceTag>> GetDataByDeviceId(int deviceId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByChannel(string channel)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByDeviceName(string channel, string deviceName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataActive(string channel, string deviceName, bool isActive)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeviceTag>> GetDataByAccessType(string accessType)
    {
        throw new NotImplementedException();
    }
}