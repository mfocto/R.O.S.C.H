using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IDeviceRepository
{
    Task<Device> GetDevice(NpgsqlConnection conn, string alias);
    Task<IEnumerable<Device>> GetDevicesByDeviceCode(NpgsqlConnection conn, string deviceType, string deviceCode);
    Task<IEnumerable<Device>> GetDevices(NpgsqlConnection conn);
    Task<IEnumerable<Device>> GetDevicesByDeviceType(NpgsqlConnection conn, string deviceType);
    Task<int> CreateDevice(NpgsqlConnection conn, NpgsqlTransaction tx, Device device);
    Task<int> UpdateDevice(NpgsqlConnection conn, NpgsqlTransaction tx, Device device);
    Task<int> DeleteDevice(NpgsqlConnection conn, NpgsqlTransaction tx, int deviceId);
}