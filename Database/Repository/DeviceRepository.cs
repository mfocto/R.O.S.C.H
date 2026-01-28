using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class DeviceRepository: IDeviceRepository
{
    private readonly string _connectionString;
    
    public DeviceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<Device> GetDevice(string alias)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceCode(string deviceType, string deviceCode)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevices()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceType(string deviceType)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateDevice(Device device)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateDevice(Device device)
    {
        throw new NotImplementedException();
    }

    public Task<int> DeleteDevice(int deviceId)
    {
        throw new NotImplementedException();
    }
}