using Npgsql;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class CodeRepository : ICodeRepository
{
    private readonly string _connectionString;
    
    public CodeRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }


    public Task<IEnumerable<Code>> GetCodesByType(NpgsqlConnection conn, string type)
    {
        throw new NotImplementedException();
    }

    public Task<Code> GetCode(NpgsqlConnection conn, string code)
    {
        throw new NotImplementedException();
    }
}