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

    public Task<Device> GetDevice(NpgsqlConnection conn, string alias)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceCode(NpgsqlConnection conn, string deviceType, string deviceCode)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevices(NpgsqlConnection conn)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceType(NpgsqlConnection conn, string deviceType)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateDevice(NpgsqlConnection conn, NpgsqlTransaction tx, Device device)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateDevice(NpgsqlConnection conn, NpgsqlTransaction tx, Device device)
    {
        throw new NotImplementedException();
    }

    public Task<int> DeleteDevice(NpgsqlConnection conn, NpgsqlTransaction tx, int deviceId)
    {
        throw new NotImplementedException();
    }
}