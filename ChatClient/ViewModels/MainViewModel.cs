using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatClient.Base;
using ChatClient.Models;
using ChatClient.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json; // Для обработки dynamic

namespace ChatClient.ViewModels
{
    // ViewModel для основного окна чата
    public class MainViewModel : BaseViewModel
    {
        private Models.User? _currentUser;
        private Models.User? _selectedUser;
        private string _chatHeaderText = "Выберите пользователя для начала общения";
        private string _messageInputText = "Введите сообщение...";
        private bool _isMessageInputPlaceholder = true;
        private string _searchUsersText = "Поиск пользователей...";
        private bool _isSearchUsersPlaceholder = true;

        private readonly HubConnection _hubConnection;
        private readonly ChatService _chatService;
        private readonly Action _onOpenAdminPanel;
        private readonly Action<string, string> _showNotification;

        public ObservableCollection<Models.User> Users { get; } = new ObservableCollection<Models.User>();
        public ObservableCollection<Models.Message> Messages { get; } = new ObservableCollection<Models.Message>();
        public ObservableCollection<Models.User> NotificationUsers { get; } = new ObservableCollection<Models.User>();

        public Models.User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public Models.User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    if (_selectedUser != null)
                    {
                        ChatHeaderText = $"Чат с {_selectedUser.Username}";
                        LoadConversation(_selectedUser.Id);
                    }
                    else
                    {
                        ChatHeaderText = "Выберите пользователя для начала общения";
                        Messages.Clear();
                    }
                }
            }
        }

        public string ChatHeaderText
        {
            get => _chatHeaderText;
            set => SetProperty(ref _chatHeaderText, value);
        }

        public string MessageInputText
        {
            get => _messageInputText;
            set
            {
                if (SetProperty(ref _messageInputText, value))
                {
                    IsMessageInputPlaceholder = string.IsNullOrWhiteSpace(value) || value == "Введите сообщение...";
                }
            }
        }

        public bool IsMessageInputPlaceholder
        {
            get => _isMessageInputPlaceholder;
            set => SetProperty(ref _isMessageInputPlaceholder, value);
        }

        public string SearchUsersText
        {
            get => _searchUsersText;
            set
            {
                if (SetProperty(ref _searchUsersText, value))
                {
                    IsSearchUsersPlaceholder = string.IsNullOrWhiteSpace(value) || value == "Поиск пользователей...";
                    FilterUsers(value);
                }
            }
        }

        public bool IsSearchUsersPlaceholder
        {
            get => _isSearchUsersPlaceholder;
            set => SetProperty(ref _isSearchUsersPlaceholder, value);
        }

        public ICommand SendMessageCommand { get; }
        public ICommand LoadUsersCommand { get; }
        public ICommand AddNotificationSubscriptionCommand { get; }
        public ICommand RemoveNotificationSubscriptionCommand { get; }
        public ICommand OpenAdminPanelCommand { get; }
        public ICommand ToggleNotificationSubscriptionCommand { get; }

        public MainViewModel(HubConnection hubConnection, ChatService chatService, Action onOpenAdminPanel, Action<string, string> showNotification)
        {
            _hubConnection = hubConnection;
            _chatService = chatService;
            _onOpenAdminPanel = onOpenAdminPanel;
            _showNotification = showNotification;

            SendMessageCommand = new RelayCommand(async _ => await ExecuteSendMessage());
            LoadUsersCommand = new RelayCommand(async _ => await ExecuteLoadUsers());
            AddNotificationSubscriptionCommand = new RelayCommand(async parameter => await ExecuteAddNotificationSubscription(parameter));
            RemoveNotificationSubscriptionCommand = new RelayCommand(async parameter => await ExecuteRemoveNotificationSubscription(parameter));
            OpenAdminPanelCommand = new RelayCommand(_ => ExecuteOpenAdminPanel());
            ToggleNotificationSubscriptionCommand = new RelayCommand(async parameter => await ExecuteToggleNotificationSubscription(parameter));

            SetupSignalRHandlers();
        }

        public async Task Initialize(Models.User currentUser)
        {

            CurrentUser = currentUser;
            await ExecuteLoadUsers();
            await LoadNotificationSubscriptions();
        }

        private void SetupSignalRHandlers()
        {

            _hubConnection.On<dynamic>("ReceiveMessage", (messageData) =>
            {
                var message = new Models.Message
                {
                    Id = messageData.GetProperty("id").GetInt32(),
                    SenderId = messageData.GetProperty("senderId").GetInt32(),
                    SenderUsername = messageData.GetProperty("senderUsername").GetString(),
                    Content = messageData.GetProperty("content").GetString(),
                    SentAt = messageData.GetProperty("sentAt").GetDateTime(),
                    IsFromCurrentUser = false
                };

                // Добавляем сообщение только если это текущий выбранный чат
                if (SelectedUser != null && message.SenderId == SelectedUser.Id)
                {
                    App.Current.Dispatcher.Invoke(() => Messages.Add(message));
                    // Здесь UI должен автоматически прокрутиться вниз, если привязка к ScrollViewer настроена
                }
                _showNotification?.Invoke($"Новое сообщение от {message.SenderUsername}", message.Content);
            });

            _hubConnection.On<dynamic>("MessageSent", (messageData) =>
            {
                // Сообщение уже добавлено в Messages в ExecuteSendMessage
                // Это подтверждение от сервера, можно использовать для обновления статуса сообщения (например, "отправлено")
            });

            _hubConnection.On<int, string, bool>("UserStatusChanged", (userId, username, isOnline) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var user = Users.FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                    {
                        user.IsOnline = isOnline; // Обновляем свойство, UI обновится автоматически
                        var status = isOnline ? "подключился" : "отключился";
                        _showNotification?.Invoke($"{username} {status}", "");
                    }
                    var notificationUser = NotificationUsers.FirstOrDefault(u => u.Id == userId);
                    if (notificationUser != null)
                    {
                        notificationUser.IsOnline = isOnline;
                    }
                });
            });

            _hubConnection.On<dynamic[]>("UserListLoaded", (usersData) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var userData in usersData)
                    {
                        // Исправление: Явно указываем тип JsonElement для lastSeenProp
                        JsonElement lastSeenProp;
                        DateTime? lastSeen = null;

                        if (userData.TryGetProperty("lastSeen", out lastSeenProp) && lastSeenProp.ValueKind != JsonValueKind.Null)
                        {
                            lastSeen = lastSeenProp.GetDateTime();
                        }

                        Users.Add(new Models.User
                        {
                            Id = userData.GetProperty("id").GetInt32(),
                            Username = userData.GetProperty("username").GetString(),
                            IsOnline = userData.GetProperty("isOnline").GetBoolean(),
                            LastSeen = lastSeen
                        });
                    }
                });
            });

            _hubConnection.On<int, dynamic[]>("ConversationLoaded", (partnerId, messagesData) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (SelectedUser?.Id == partnerId)
                    {
                        Messages.Clear();
                        foreach (var messageData in messagesData)
                        {
                            Messages.Add(new Models.Message
                            {
                                Id = messageData.GetProperty("id").GetInt32(),
                                SenderId = messageData.GetProperty("senderId").GetInt32(),
                                SenderUsername = messageData.GetProperty("senderUsername").GetString(),
                                ReceiverId = messageData.GetProperty("receiverId").GetInt32(),
                                ReceiverUsername = messageData.GetProperty("receiverUsername").GetString(),
                                Content = messageData.GetProperty("content").GetString(),
                                SentAt = messageData.GetProperty("sentAt").GetDateTime(),
                                IsRead = messageData.GetProperty("isRead").GetBoolean(),
                                IsFromCurrentUser = messageData.GetProperty("senderId").GetInt32() == CurrentUser?.Id
                            });
                        }
                    }
                });
            });

            _hubConnection.On<dynamic[]>("NotificationSubscriptionsLoaded", (subscriptionsData) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    NotificationUsers.Clear();
                    var subscribedIds = new HashSet<int>();
                      foreach (var subData in subscriptionsData)
                    {
                        // Исправление: Явно указываем тип JsonElement для lastSeenProp
                        JsonElement lastSeenProp;
                        DateTime? lastSeen = null;

                        if (subData.TryGetProperty("lastSeen", out lastSeenProp) && lastSeenProp.ValueKind != JsonValueKind.Null)
                        {
                            lastSeen = lastSeenProp.GetDateTime();
                        }

                        NotificationUsers.Add(new Models.User
                        {
                            Id = subData.GetProperty("id").GetInt32(),
                            Username = subData.GetProperty("username").GetString(),
                            IsOnline = subData.GetProperty("isOnline").GetBoolean(),
                            LastSeen = lastSeen
                        });
                    }
                });
            });

            _hubConnection.On<int, bool>("NotificationSubscriptionAdded", (targetUserId, success) =>
            {
                if (success)
                {
                    App.Current.Dispatcher.Invoke(async () => await LoadNotificationSubscriptions());
                    _showNotification?.Invoke("Уведомление", $"Подписка на пользователя ID {targetUserId} добавлена.");
                }
                else
                {
                    _showNotification?.Invoke("Ошибка", $"Не удалось добавить подписку на пользователя ID {targetUserId}.");
                }
            });

            _hubConnection.On<int, bool>("NotificationSubscriptionRemoved", (targetUserId, success) =>
            {
                if (success)
                {
                    App.Current.Dispatcher.Invoke(async () => await LoadNotificationSubscriptions());
                    _showNotification?.Invoke("Уведомление", $"Подписка на пользователя ID {targetUserId} удалена.");
                }
                else
                {
                    _showNotification?.Invoke("Ошибка", $"Не удалось удалить подписку на пользователя ID {targetUserId}.");
                }
            });
        }

        private async Task ExecuteSendMessage()
        {
            if (SelectedUser == null || string.IsNullOrWhiteSpace(MessageInputText) || IsMessageInputPlaceholder)
            {
                return;
            }

            try
            {
                var messageContent = MessageInputText;
                MessageInputText = "Введите сообщение..."; // Сбрасываем текст
                IsMessageInputPlaceholder = true; // Устанавливаем флаг плейсхолдера

                // Отправляем сообщение через SignalR
                await _hubConnection.InvokeAsync("SendMessage", SelectedUser.Id, messageContent);

                // Добавляем сообщение в UI сразу, чтобы пользователь видел его
                // Серверное подтверждение (MessageSent) может быть использовано для обновления ID сообщения или статуса
                App.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(new Models.Message
                    {
                        SenderId = CurrentUser!.Id,
                        SenderUsername = CurrentUser.Username,
                        ReceiverId = SelectedUser.Id,
                        ReceiverUsername = SelectedUser.Username,
                        Content = messageContent,
                        SentAt = DateTime.UtcNow,
                        IsFromCurrentUser = true
                    });
                });
            }
            catch (Exception ex)
            {
                _showNotification?.Invoke("Ошибка отправки сообщения", ex.Message);
            }
        }

        private async Task ExecuteLoadUsers()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("GetUserList");
                }
                catch (Exception ex)
                {
                    _showNotification?.Invoke("Ошибка загрузки пользователей", ex.Message);
                }
            }
        }

        private async Task LoadConversation(int partnerId)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    // ИСПРАВЛЕНО: передаем только partnerId, userId берется на сервере из _connectionUserMap
                    await _hubConnection.InvokeAsync("GetConversation", partnerId, 0, 50);
                }
                catch (Exception ex)
                {
                    _showNotification?.Invoke("Ошибка загрузки переписки", ex.Message);
                }
            }
        }

        private async Task LoadNotificationSubscriptions()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("GetNotificationSubscriptions");
                }
                catch (Exception ex)
                {
                    _showNotification?.Invoke("Ошибка загрузки подписок", ex.Message);
                }
            }
        }

        private async Task ExecuteAddNotificationSubscription(object? parameter)
        {
            if (parameter is Models.User user && CurrentUser != null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("AddNotificationSubscription", user.Id);
                }
                catch (Exception ex)
                {
                    _showNotification?.Invoke("Ошибка подписки", ex.Message);
                }
            }
        }

        private async Task ExecuteRemoveNotificationSubscription(object? parameter)
        {
            if (parameter is Models.User user && CurrentUser != null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("RemoveNotificationSubscription", user.Id);
                }
                catch (Exception ex)
                {
                    _showNotification?.Invoke("Ошибка отписки", ex.Message);
                }
            }
        }

        private void ExecuteOpenAdminPanel()
        {
            _onOpenAdminPanel?.Invoke();
        }

        private void FilterUsers(string searchText)
        {
            // Здесь можно реализовать логику фильтрации, если Users привязан к CollectionViewSource
            // Для ObservableCollection можно временно очистить и добавить отфильтрованные элементы,
            // но это может быть неэффективно для больших списков.
            // Более правильный подход - использовать ICollectionView.
            // Пока оставим это как заглушку, так как SignalR будет загружать полный список.
        }

        private async Task ExecuteToggleNotificationSubscription(object? parameter)
        {
            if (parameter is Models.User user && CurrentUser != null)
            {
                if (user.IsSubscribed)
                    await ExecuteRemoveNotificationSubscription(user);
                else
                    await ExecuteAddNotificationSubscription(user);
            }
        }
    }
}
