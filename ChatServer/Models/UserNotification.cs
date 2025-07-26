namespace ChatServer.Models
{
    public class UserNotification
    {
        public int Id { get; set; }

        // User who wants to receive notifications
        public int SubscriberId { get; set; }
        public virtual User Subscriber { get; set; } = null!;

        // User whose status changes trigger notifications
        public int TargetUserId { get; set; }
        public virtual User TargetUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
