using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IDeviceRepository
{
    Task<Device> GetDevice(string alias);
    Task<IEnumerable<Device>> GetDevicesByDeviceCode(string deviceType, string deviceCode);
    Task<IEnumerable<Device>> GetDevices();
    Task<IEnumerable<Device>> GetDevicesByDeviceType(string deviceType);
    Task<int> CreateDevice(Device device);
    Task<int> UpdateDevice(Device device);
    Task<int> DeleteDevice(int deviceId);
}