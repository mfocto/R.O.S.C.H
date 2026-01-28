using R.O.S.C.H.Database.Models;

namespace R.O.S.C.H.Database.Repository.Interface;

public interface IUserRepository
{
    Task<User?> GetUserByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<int> CreateAsync(User user);
    Task<int> UpdateAsync(User user);
    Task<int> DeleteAsync(int userId);
}