using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ChatServer.Models
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Message entity configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // UserNotification entity configuration
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(un => un.Id);

                entity.HasOne(un => un.Subscriber)
                    .WithMany(u => u.NotificationSubscriptions)
                    .HasForeignKey(un => un.SubscriberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(un => un.TargetUser)
                    .WithMany(u => u.NotificationTargets)
                    .HasForeignKey(un => un.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate subscriptions
                entity.HasIndex(un => new { un.SubscriberId, un.TargetUserId }).IsUnique();
            });
        }
    }
}
