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
    
    
    public async Task<User?> GetUserByUserNameAsync(NpgsqlConnection conn, string Username)
    {
        string sql = """
                     SELECT user_id as UserId,
                            username as UserName, 
                            password as Password, 
                            role as Role, 
                            is_active as IsActive,
                            created_at as  CreatedAt,  
                            updated_at as UpdatedAt
                     FROM   "user"
                     WHERE  username = @Username
                     """;

        return await conn.QueryFirstOrDefaultAsync<User>(sql, new {Username = Username});
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync(NpgsqlConnection conn)
    {
        // demo 버전에서만 사용 
        string sql = """
                     SELECT user_id as UserId,  
                            username as UserName, 
                            password as Password, 
                            role as Role, 
                            is_active as IsActive,
                            created_at as  CreatedAt,  
                            updated_at as UpdatedAt
                     FROM  "user"
                     ORDER BY user_id
                     """;
        
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<int> CreateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, User user)
    {
        string sql = """
                     INSERT INTO "user" (username, password, role, is_active, created_at, updated_at)
                     VALUES (@Username, @Password, @Role, @IsActive, @CreatedAt, @UpdatedAt)
                     RETURNING user_id
                     """;
        
        return await conn.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task<int> UpdateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, User user)
    {
        string sql = """
                     UPDATE "user"
                     SET    is_active = @IsActive,
                            updated_at = @UpdatedAt
                     WHERE  user_id = @UserId  
                     """;
        
        return await conn.ExecuteAsync(sql, user);
    }
}