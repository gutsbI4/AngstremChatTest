using ChatServer.Models;

namespace ChatServer.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User?> RegisterAsync(string username, string email, string password);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> SetUserOnlineStatusAsync(int userId, bool isOnline);
        Task<bool> AddNotificationSubscriptionAsync(int subscriberId, int targetUserId);
        Task<bool> RemoveNotificationSubscriptionAsync(int subscriberId, int targetUserId);
        Task<List<User>> GetNotificationSubscribersAsync(int userId);
        Task<List<User>> GetSubscriptionsAsync(int userId);
        Task<bool> DeleteUserAsync(int userId);
    }
}
