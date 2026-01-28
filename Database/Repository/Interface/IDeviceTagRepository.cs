using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IDeviceTagRepository
{
    // CUD 처리는 DBMS에서 
    // READ 만 구현
    
    Task<IEnumerable<DeviceTag>> GetDataByDeviceId(int deviceId);
    Task<IEnumerable<DeviceTag>> GetDataByChannel(string channel);
    Task<IEnumerable<DeviceTag>> GetDataByDeviceName(string channel, string deviceName);
    Task<IEnumerable<DeviceTag>> GetDataActive(string channel, string deviceName, bool isActive);
    Task<IEnumerable<DeviceTag>> GetDataByAccessType(string accessType);
}