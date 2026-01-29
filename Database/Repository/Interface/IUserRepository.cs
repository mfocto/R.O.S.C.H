using Npgsql;
using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IUserRepository
{
    Task<User?> GetUserByUserNameAsync(NpgsqlConnection conn, string userName);
    Task<IEnumerable<User>> GetAllUsersAsync(NpgsqlConnection conn);
    Task<int> CreateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, User user);
    Task<int> UpdateAsync(NpgsqlConnection conn, NpgsqlTransaction tx, User user);
}