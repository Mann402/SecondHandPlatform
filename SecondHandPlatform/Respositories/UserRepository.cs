using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;

namespace SecondHandPlatform.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SecondhandplatformContext _context;

        public UserRepository(SecondhandplatformContext context)
        {
            _context = context;
        }

        public async Task<bool> UserExists(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                // Log what we're updating (don't log passwords)
                Console.WriteLine($"Updating user in repository: ID={user.UserId}, FirstName={user.FirstName}, LastName={user.LastName}");

                // Don't need to call Update if the entity is already tracked
                // _context.Users.Update(user);

                // Instead, mark entity as modified
                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                Console.WriteLine($"User {user.UserId} updated successfully in repository");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in repository updating user {user.UserId}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw to be handled by the service
            }
        }

        public async Task DeleteUserAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
