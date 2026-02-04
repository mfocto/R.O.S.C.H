using Dapper;
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

    public async Task<IEnumerable<DeviceTag>> GetDataByDeviceId(NpgsqlConnection conn, int deviceId)
    {
        string sql = """
                     SELECT     tag_id AS TagId,
                                device_id AS DeviceId,
                                channel AS Channel,
                                device_name AS DeviceName,
                                tag AS Tag,
                                access_type AS AccessType,
                                data_type AS DataType,
                                description AS Description,
                                is_active AS IsActive,
                                created_at AS CreatedAt
                     FROM       "device_tag"
                     WHERE     device_id = @DeviceId
                     """;
        return await conn.QueryAsync<DeviceTag>(sql, new {DeviceId = deviceId});
    }

    public async Task<IEnumerable<DeviceTag>> GetDataByChannel(NpgsqlConnection conn, string channel)
    {
        string sql = """
                     SELECT     tag_id AS TagId,
                                device_id AS DeviceId,
                                channel AS Channel,
                                device_name AS DeviceName,
                                tag AS Tag,
                                access_type AS AccessType,
                                data_type AS DataType,
                                description AS Description,
                                is_active AS IsActive,
                                created_at AS CreatedAt
                     FROM       "device_tag"
                     WHERE      channel = @Channel
                     """;
        return await conn.QueryAsync<DeviceTag>(sql, new {Channel = channel});
    }

    public async Task<IEnumerable<DeviceTag>> GetDataByDeviceName(NpgsqlConnection conn, string channel, string deviceName)
    {
        string sql = """
                     SELECT     tag_id AS TagId,
                                device_id AS DeviceId,
                                channel AS Channel,
                                device_name AS DeviceName,
                                tag AS Tag,
                                access_type AS AccessType,
                                data_type AS DataType,
                                description AS Description,
                                is_active AS IsActive,
                                created_at AS CreatedAt
                     FROM       "device_tag"
                     WHERE      channel = @Channel
                     AND        device_name = @DeviceName  
                     """;
        return await conn.QueryAsync<DeviceTag>(sql, new {Channel = channel, DeviceName = deviceName});
    }

    public async Task<IEnumerable<DeviceTag>> GetDataActive(NpgsqlConnection conn, string channel, string deviceName, bool isActive)
    {
        string sql = """
                     SELECT     tag_id AS TagId,
                                device_id AS DeviceId,
                                channel AS Channel,
                                device_name AS DeviceName,
                                tag AS Tag,
                                access_type AS AccessType,
                                data_type AS DataType,
                                description AS Description,
                                is_active AS IsActive,
                                created_at AS CreatedAt
                     FROM       "device_tag"
                     WHERE      channel = @Channel
                     AND        device_name = @DeviceName
                     AND        is_active = @IsActive
                     """;
        return await conn.QueryAsync<DeviceTag>(sql, new{Channel = channel, DeviceName = deviceName, IsActive = isActive});
    }

    public async Task<IEnumerable<DeviceTag>> GetDataByAccessType(NpgsqlConnection conn, string accessType)
    {
        string sql = """
                     SELECT     tag_id AS TagId,
                                device_id AS DeviceId,
                                channel AS Channel,
                                device_name AS DeviceName,
                                tag AS Tag,
                                access_type AS AccessType,
                                data_type AS DataType,
                                description AS Description,
                                is_active AS IsActive,
                                created_at AS CreatedAt
                     FROM       "device_tag"
                     WHERE      access_type = @AccessType
                     """;
        return await conn.QueryAsync<DeviceTag>(sql, new {AccessType = accessType});
    }
}