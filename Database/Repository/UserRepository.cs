using Dapper;
using Npgsql;
using Opc.Ua;
using R.O.S.C.H.Database.Models;
using R.O.S.C.H.Database.Repository.Interface;

namespace R.O.S.C.H.Database.Repository;

public class UserRepository: IUserRepository
{
    private readonly string _connectionString;
    public UserRepository (IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new ArgumentNullException("ConnectionString");
    }
    
    
    public async Task<User?> GetUserByUserNameAsync(NpgsqlConnection conn)
    {
        string sql = """
                     SELECT user_id as UserId,  username as UserName, password as Password, role as Role, is_active as IsActive,
                     created_at as  CreatedAt,  updated_at as UpdatedAt
                     FROM ""user""
                     WHERE username = @Username
                     """;

        return await conn.QueryFirstOrDefaultAsync<User>(sql, new {Username = userName});
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        // 데모버전에서만 사용할 메서드
    }

    public async Task<int> CreateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public async Task<int> UpdateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public async Task<int> DeleteAsync(int userId)
    {
        throw new NotImplementedException();
    }
}