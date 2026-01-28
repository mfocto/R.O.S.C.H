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
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    } 
    
    public Task<IEnumerable<Code>> GetCodesByType(string type)
    {
        throw new NotImplementedException();
    }

    public Task<Code> GetCode(string code)
    {
        throw new NotImplementedException();
    }
}