using System;
using System.ComponentModel;
using ChatClient.Base;

namespace ChatClient.Models
{
    public class User : BaseViewModel
    {
        private bool _isOnline;
        private DateTime? _lastSeen;
        private bool _isSubscribed;

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public DateTime? LastSeen
        {
            get => _lastSeen;
            set => SetProperty(ref _lastSeen, value);
        }

        public bool IsSubscribed
        {
            get => _isSubscribed;
            set => SetProperty(ref _isSubscribed, value);
        }
    }
}
