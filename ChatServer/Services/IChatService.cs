using ChatServer.Models;

namespace ChatServer.Services
{
    public interface IChatService
    {
        Task<Message> SendMessageAsync(int senderId, int receiverId, string content);
        Task<List<Message>> GetConversationAsync(int user1Id, int user2Id, int skip = 0, int take = 50);
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<List<Message>> GetUnreadMessagesAsync(int userId);
        Task<List<User>> GetConversationPartnersAsync(int userId);
    }
}
