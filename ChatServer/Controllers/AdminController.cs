using Microsoft.AspNetCore.Mvc;
using ChatServer.Services;
using ChatServer.Models;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IChatService _chatService;

        public AdminController(IUserService userService, IChatService chatService)
        {
            _userService = userService;
            _chatService = chatService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            var userData = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt,
                u.IsOnline,
                u.LastSeen
            });

            return Ok(userData);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.CreatedAt,
                user.IsOnline,
                user.LastSeen
            });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
                return NotFound();

            return Ok();
        }

        [HttpGet("users/{id}/conversations")]
        public async Task<IActionResult> GetUserConversations(int id)
        {
            var partners = await _chatService.GetConversationPartnersAsync(id);
            return Ok(partners.Select(p => new
            {
                p.Id,
                p.Username,
                p.IsOnline,
                p.LastSeen
            }));
        }

        [HttpGet("users/{userId}/conversation/{partnerId}")]
        public async Task<IActionResult> GetConversation(int userId, int partnerId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var messages = await _chatService.GetConversationAsync(userId, partnerId, skip, take);
            var messageData = messages.Select(m => new
            {
                m.Id,
                SenderId = m.Sender.Id,
                SenderUsername = m.Sender.Username,
                ReceiverId = m.Receiver.Id,
                ReceiverUsername = m.Receiver.Username,
                m.Content,
                m.SentAt,
                m.IsRead
            });

            return Ok(messageData);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var users = await _userService.GetAllUsersAsync();
            var onlineCount = users.Count(u => u.IsOnline);

            return Ok(new
            {
                TotalUsers = users.Count,
                OnlineUsers = onlineCount,
                OfflineUsers = users.Count - onlineCount,
                RecentUsers = users.Where(u => u.LastSeen.HasValue && u.LastSeen.Value > DateTime.UtcNow.AddDays(-7)).Count()
            });
        }
    }
}