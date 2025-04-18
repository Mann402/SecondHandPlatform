using SecondHandPlatform.Models;
using System.Threading.Tasks;

public interface IUserRepository
{
    Task<bool> UserExists(string email);
    Task CreateUserAsync(User user);
    Task<User> GetUserByEmailAsync(string email);
    Task<User> GetUserByIdAsync(int id);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(User user);
}
