using System.Threading.Tasks;
using SecondHandPlatform.Models;
using SecondHandPlatform.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using SecondHandPlatform.DTOs;
using Microsoft.EntityFrameworkCore;

namespace SecondHandPlatform.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool success, string message)> RegisterUserAsync(User user)
        {
            // 1) Validate Email Domain
            if (!IsValidEmailDomain(user.Email))
                return (false, "Invalid email domain. Must be a @gmail.com or @student.tarc.edu.my address.");

            // 2) Check if email already exists
            if (await _userRepository.UserExists(user.Email))
                return (false, "Email already exists.");

            // 3) Hash Password, Save User
            user.Password = HashPassword(user.Password);
            await _userRepository.CreateUserAsync(user);
            return (true, "User registered successfully!");
        }


        public async Task<User> LoginUserAsync(User loginUser)
        {
            var user = await _userRepository.GetUserByEmailAsync(loginUser.Email);
            if (user == null)
            {
                Console.WriteLine("User not found with email: " + loginUser.Email);
                return null;
            }

            Console.WriteLine("User found: " + user.Email);

            if (!VerifyPassword(loginUser.Password, user.Password))
            {
                Console.WriteLine("Password mismatch for user: " + loginUser.Email);
                return null;
            }

            Console.WriteLine("Login successful for: " + user.Email);
            return user;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }

        public async Task<(bool success, string message)> UpdateUserAsync(int id, UserUpdateDto userDto)
        {
            try
            {
                // Get the current user
                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Log what we're updating
                Console.WriteLine($"Updating user {id}: First Name={userDto.FirstName}, Last Name={userDto.LastName}");

                // Update basic fields
                user.FirstName = userDto.FirstName;
                user.LastName = userDto.LastName;

                // Handle password update if provided
                if (!string.IsNullOrEmpty(userDto.NewPassword))
                {
                    if (string.IsNullOrEmpty(userDto.OldPassword))
                    {
                        Console.WriteLine("Password update failed: Old password not provided");
                        return (false, "Current password is required to update to a new password.");
                    }

                    // Verify the old password
                    if (!VerifyPassword(userDto.OldPassword, user.Password))
                    {
                        Console.WriteLine("Password update failed: Old password incorrect");
                        return (false, "Current password is incorrect.");
                    }

                    // Update password: store the hash of the new password
                    Console.WriteLine("Updating password");
                    user.Password = HashPassword(userDto.NewPassword);
                }

                // Don't update other fields like Email, StudentCardPicture, UserStatus
                // These should remain as they are

                try
                {
                    // Save the updated user
                    await _userRepository.UpdateUserAsync(user);
                    Console.WriteLine($"User {id} updated successfully");
                    return (true, "User updated successfully!");
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"Database update error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    return (false, "Database error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Log any unexpected errors
                Console.WriteLine($"Unexpected error updating user {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, "An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<(bool success, string message)> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return (false, "User not found.");

            await _userRepository.DeleteUserAsync(user);
            return (true, "User deleted successfully!");
        }
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            // Hash the entered password
            string hashedPassword = HashPassword(enteredPassword);

            // Compare the hashed entered password with the stored hashed password
            return hashedPassword == storedHash;
        }

        private bool IsValidEmailDomain(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            // Option A: Simple domain check
            // Return true only if it ends with @gmail.com or @student.tarc.edu.my
            email = email.ToLower();
            return email.EndsWith("@gmail.com") || email.EndsWith("@student.tarc.edu.my");

            // Option B: Regex approach (uncomment if you prefer)
            /*
            var pattern = @"^[^@\s]+@(gmail\.com|student\.tarc\.edu\.my)$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            */
        }
    }
}
