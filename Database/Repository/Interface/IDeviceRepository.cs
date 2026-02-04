using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IDeviceRepository
{
    Task<Device?> GetDevice(NpgsqlConnection conn, string alias);
    Task<IEnumerable<Device>> GetDevicesByDeviceCode(NpgsqlConnection conn, string deviceType, string deviceCode);
    Task<IEnumerable<Device>> GetDevicesByDeviceType(NpgsqlConnection conn, string deviceType);
}