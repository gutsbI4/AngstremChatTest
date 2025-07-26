using System.ComponentModel.DataAnnotations;

namespace ChatServer.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsOnline { get; set; } = false;

        public DateTime? LastSeen { get; set; }

        // Navigation properties
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<UserNotification> NotificationSubscriptions { get; set; } = new List<UserNotification>();
        public virtual ICollection<UserNotification> NotificationTargets { get; set; } = new List<UserNotification>();
    }
}
