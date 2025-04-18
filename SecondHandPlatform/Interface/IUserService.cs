using SecondHandPlatform.DTOs;
using SecondHandPlatform.Models;
using System.Threading.Tasks;

public interface IUserService
{
    Task<(bool success, string message)> RegisterUserAsync(User user);
    Task<User> LoginUserAsync(User loginUser);
    Task<User> GetUserByIdAsync(int id);
    Task<(bool success, string message)> UpdateUserAsync(int id, UserUpdateDto userDto);
    Task<(bool success, string message)> DeleteUserAsync(int id);
}
