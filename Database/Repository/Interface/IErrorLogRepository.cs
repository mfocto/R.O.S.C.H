using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IErrorLogRepository
{
    Task<IEnumerable<ErrorLog>> GetErrorLogsBySource(string errorSource, DateTimeOffset logTime);
    Task<IEnumerable<ErrorLog>> GetErrorLogsByDevice(int deviceId, DateTimeOffset logTime);
    Task<IEnumerable<ErrorLog>> GetErrorLogsByCode(string errorCode, DateTimeOffset logTime);
    
    Task<int> CreateErrorLog(ErrorLog errorLog);
    Task<int> UpdateErrorLog(ErrorLog errorLog);
}