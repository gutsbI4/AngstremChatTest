namespace ChatClient.Models
{
    // Модель для хранения статистики пользователей
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int OfflineUsers { get; set; }
        public int RecentUsers { get; set; } // Например, активные за последнюю неделю
    }
}
