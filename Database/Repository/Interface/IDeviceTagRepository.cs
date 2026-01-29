using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IDeviceTagRepository
{
    // CUD 처리는 DBMS에서 
    // READ 만 구현
    
    Task<IEnumerable<DeviceTag>> GetDataByDeviceId(NpgsqlConnection conn, int deviceId);
    Task<IEnumerable<DeviceTag>> GetDataByChannel(NpgsqlConnection conn, string channel);
    Task<IEnumerable<DeviceTag>> GetDataByDeviceName(NpgsqlConnection conn, string channel, string deviceName);
    Task<IEnumerable<DeviceTag>> GetDataActive(NpgsqlConnection conn, string channel, string deviceName, bool isActive);
    Task<IEnumerable<DeviceTag>> GetDataByAccessType(NpgsqlConnection conn, string accessType);
}