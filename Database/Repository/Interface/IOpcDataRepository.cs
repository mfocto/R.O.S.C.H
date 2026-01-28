using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IOpcDataRepository
{
    // deviceId 로 데이터 조회
    Task<IEnumerable<OpcData>> GetDataByDeviceId(int deviceId);
    // device + tag 로 데이터 조회
    Task<IEnumerable<OpcData>> GetDataByDeviceTag(int deviceId, string tag);
    // 시간으로 조회
    Task<IEnumerable<OpcData>> GetDataBySourceTime(DateTimeOffset time);

    // 등록
    Task<int> CreateOpcData(OpcData opcData);
}