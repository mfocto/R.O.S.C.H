using Dapper;
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

    public async Task<Device?> GetDevice(NpgsqlConnection conn, string alias)
    {
        string sql = """
                     SELECT device_id AS DeviceId,
                            device_code AS DeviceCode,
                            device_type AS DeviceType,
                            device_alias AS DeviceAlias,
                            created_at AS CreatedAt
                     FROM   "device"
                     WHERE  device_alias = @Alias
                     """;
        return await conn.QueryFirstOrDefaultAsync<Device>(sql, new { Alias = alias });
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceCode(NpgsqlConnection conn, string deviceType, string deviceCode)
    {
        string sql = """
                     SELECT device_id AS DeviceId,
                            device_code AS DeviceCode,
                            device_type AS DeviceType,
                            device_alias AS DeviceAlias,
                            created_at AS CreatedAt
                     FROM   "device"
                     WHERE  device_type = @DeviceType
                     AND    device_code = @DeviceCode
                     """;
        return conn.QueryAsync<Device>(sql, new { DeviceType = deviceType, DeviceCode = deviceCode });
    }

    public Task<IEnumerable<Device>> GetDevicesByDeviceType(NpgsqlConnection conn, string deviceType)
    {
        string sql = """
                     SELECT device_id AS DeviceId,
                            device_code AS DeviceCode,
                            device_type AS DeviceType,
                            device_alias AS DeviceAlias,
                            created_at AS CreatedAt
                     FROM   "device"
                     WHERE  device_type = @DeviceType
                     """;
        return conn.QueryAsync<Device>(sql, new { DeviceType = deviceType });
    }

}