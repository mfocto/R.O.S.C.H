using Dapper;
using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class OpcDataRepository: IOpcDataRepository
{
    private readonly string _connectionString;
    
    public OpcDataRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }

    public async Task<IEnumerable<OpcData>> GetDataByDeviceId(NpgsqlConnection conn, int deviceId)
    {
        string sql = """
                     SELECT     data_id as DataId,
                                device_id as DeviceId,
                                tag_name as TagName,
                                tag_value as TagValue,
                                data_type as DataType,
                                source_time as SourceTime,
                                created_at as CreatedAt
                     FROM       "opc_data"
                     WHERE      device_id = @DeviceId    
                     """;

        return await conn.QueryAsync<OpcData>(sql, new { DeviceId = deviceId });

    }

    public async Task<IEnumerable<OpcData>> GetDataByDeviceTag(NpgsqlConnection conn, int deviceId, string tag)
    {
        string sql = """
                     SELECT     data_id as DataId,
                                device_id as DeviceId,
                                tag_name as TagName,
                                tag_value as TagValue,
                                data_type as DataType,
                                source_time as SourceTime,
                                created_at as CreatedAt
                     FROM       "opc_data"
                     WHERE      device_id = @DeviceId
                     AND        tag = @Tag
                     """;
        
        return await conn.QueryAsync<OpcData>(sql, new { DeviceId = deviceId, Tag = tag });
    }

    public async Task<IEnumerable<OpcData>> GetDataBySourceTime(NpgsqlConnection conn, DateTimeOffset time)
    {
        string sql = """
                     SELECT     data_id as DataId,
                                device_id as DeviceId,
                                tag_name as TagName,
                                tag_value as TagValue,
                                data_type as DataType,
                                source_time as SourceTime,
                                created_at as CreatedAt
                     FROM       "opc_data"
                     WHERE     source_time >= @Time
                     """;
        
        return await conn.QueryAsync<OpcData>(sql, new { Time = time });
    }

    public async Task<int> CreateOpcData(NpgsqlConnection conn, NpgsqlTransaction tx, OpcData opcData)
    {
        string sql = """
                     INSERT INTO "opc_data" (device_id, tag_name, tag_value, data_type, source_time)
                     VALUES (@DeviceId, @TagName, @TagValue, @DataType, @SourceTime)
                     """;
        return await conn.ExecuteAsync(sql, opcData);
    }
}