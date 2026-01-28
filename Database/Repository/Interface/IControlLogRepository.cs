using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IControlLogRepository
{
    Task<IEnumerable<ControlLog>> GetLogs(string idType, int Id, DateTimeOffset logTime); // type 으로 userid인지 deviceid 인지 구분
    Task<IEnumerable<ControlLog>> GetLogs(DateTimeOffset logTime);
    
    Task<int> CreateAsync(ControlLog controlLog);
    Task<int> UpdateAsync(ControlLog controlLog);
}