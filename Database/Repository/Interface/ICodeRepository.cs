using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface ICodeRepository
{
    // CUD 처리는 DBMS에서 
    // READ 만 구현
    Task<IEnumerable<Code>> GetCodesByType(NpgsqlConnection conn, string type);
    Task<Code> GetCode(NpgsqlConnection conn, string code);
}