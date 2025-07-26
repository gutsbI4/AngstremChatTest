using ChatServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Services
{
    public class UserService : IUserService
    {
        private readonly ChatDbContext _context;

        public UserService(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> RegisterAsync(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
                return null;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.OrderBy(u => u.Username).ToListAsync();
        }

        public async Task<bool> SetUserOnlineStatusAsync(int userId, bool isOnline)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddNotificationSubscriptionAsync(int subscriberId, int targetUserId)
        {
            if (subscriberId == targetUserId) return false;

            var exists = await _context.UserNotifications
                .AnyAsync(un => un.SubscriberId == subscriberId && un.TargetUserId == targetUserId);

            if (exists) return false;

            var notification = new UserNotification
            {
                SubscriberId = subscriberId,
                TargetUserId = targetUserId
            };

            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveNotificationSubscriptionAsync(int subscriberId, int targetUserId)
        {
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(un => un.SubscriberId == subscriberId && un.TargetUserId == targetUserId);

            if (notification == null) return false;

            _context.UserNotifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetNotificationSubscribersAsync(int userId)
        {
            return await _context.UserNotifications
                .Where(un => un.TargetUserId == userId)
                .Include(un => un.Subscriber)
                .Select(un => un.Subscriber)
                .ToListAsync();
        }

        public async Task<List<User>> GetSubscriptionsAsync(int userId)
        {
            return await _context.UserNotifications
                .Where(un => un.SubscriberId == userId)
                .Include(un => un.TargetUser)
                .Select(un => un.TargetUser)
                .ToListAsync();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
