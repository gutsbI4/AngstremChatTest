using Microsoft.AspNetCore.SignalR;
using ChatServer.Services;
using ChatServer.Models;
using System.Collections.Concurrent;
using System.Linq;
using System; // Добавлено для Console.WriteLine

namespace ChatServer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IUserService _userService;
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, int> _connectionUserMap = new(); // ConnectionId -> UserId
        private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnectionsMap = new(); // UserId -> HashSet<ConnectionId>

        public ChatHub(IUserService userService, IChatService chatService)
        {
            _userService = userService;
            _chatService = chatService;
            Console.WriteLine("[Сервер] ChatHub инициализирован.");
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[Сервер] OnConnectedAsync вызван. ConnectionId: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[Сервер] OnDisconnectedAsync вызван. ConnectionId: {Context.ConnectionId}, Exception: {exception?.Message}");

            if (_connectionUserMap.TryRemove(Context.ConnectionId, out int userId))
            {
                if (_userConnectionsMap.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);

                    // Если у пользователя больше нет активных соединений, устанавливаем его оффлайн
                    if (connections.Count == 0)
                    {
                        _userConnectionsMap.TryRemove(userId, out _);
                        await _userService.SetUserOnlineStatusAsync(userId, false);

                        // Уведомляем подписчиков о том, что пользователь ушел в оффлайн
                        var user = await _userService.GetUserByIdAsync(userId); // Получаем пользователя для имени
                        if (user != null)
                        {
                            var subscribers = await _userService.GetNotificationSubscribersAsync(userId);
                            foreach (var subscriber in subscribers.Where(s => s.IsOnline))
                            {
                                await Clients.Group($"user_{subscriber.Id}").SendAsync("UserStatusChanged", user.Id, user.Username, false);
                            }
                        }
                        Console.WriteLine($"[Сервер] Пользователь ID {userId} ({user?.Username ?? "Unknown"}) отключился. ConnectionId: {Context.ConnectionId}");
                    }
                    else
                    {
                        Console.WriteLine($"[Сервер] ConnectionId '{Context.ConnectionId}' удален для пользователя {userId}, но у него еще есть {connections.Count} активных соединений.");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[Сервер] OnDisconnectedAsync: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap.");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> Login(string username, string password)
        {
            Console.WriteLine($"[Сервер] Метод Login вызван для пользователя: {username}, ConnectionId: {Context.ConnectionId}");
            var user = await _userService.AuthenticateAsync(username, password);
            if (user == null)
            {
                Console.WriteLine($"[Сервер] Вход для пользователя '{username}' не удался: неверные учетные данные.");
                await Clients.Caller.SendAsync("LoginFailed", "Неверное имя пользователя или пароль.");
                return false;
            }

            // Обновляем _connectionUserMap и _userConnectionsMap
            // Удаляем старые ConnectionId для этого пользователя, если они есть
            if (_userConnectionsMap.TryGetValue(user.Id, out var existingConnections))
            {
                foreach (var oldConnId in existingConnections.ToList()) // ToList() для безопасной итерации при удалении
                {
                    if (_connectionUserMap.TryRemove(oldConnId, out _))
                    {
                        existingConnections.Remove(oldConnId);
                        Console.WriteLine($"[Сервер] Удалено старое сопоставление ConnectionId '{oldConnId}' для пользователя {user.Id}.");
                    }
                }
            }

            // Добавляем текущее соединение
            _connectionUserMap[Context.ConnectionId] = user.Id;
            _userConnectionsMap.GetOrAdd(user.Id, new HashSet<string>()).Add(Context.ConnectionId);

            // Устанавливаем пользователя онлайн, если он не был онлайн
            if (!user.IsOnline) // Проверяем текущий статус, чтобы избежать лишних обновлений
            {
                await _userService.SetUserOnlineStatusAsync(user.Id, true);
                // Уведомляем всех, если пользователь стал онлайн
                var subscribers = await _userService.GetNotificationSubscribersAsync(user.Id);
                foreach (var subscriber in subscribers.Where(s => s.IsOnline))
                {
                    await Clients.Group($"user_{subscriber.Id}").SendAsync("UserStatusChanged", user.Id, user.Username, true);
                }
                Console.WriteLine($"[Сервер] Пользователь '{username}' (ID: {user.Id}) перешел в онлайн.");
            }
            else
            {
                Console.WriteLine($"[Сервер] Пользователь '{username}' (ID: {user.Id}) уже был онлайн.");
            }

            // Присоединяем пользователя к его личной группе
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");

            // Отправляем информацию о пользователе клиенту
            await Clients.Caller.SendAsync("LoginSuccess", new
            {
                user.Id,
                user.Username,
                user.Email,
                user.IsOnline,
                user.LastSeen // Убедитесь, что LastSeen возвращается
            });

            Console.WriteLine($"[Сервер] Пользователь '{username}' (ID: {user.Id}) успешно вошел. ConnectionId: {Context.ConnectionId}");
            return true;
        }

        public async Task SendMessage(int receiverId, string message)
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int senderId))
            {
                Console.WriteLine($"[Сервер] SendMessage: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }

            var sentMessage = await _chatService.SendMessageAsync(senderId, receiverId, message);

            // Отправляем получателю, если он онлайн
            if (_userConnectionsMap.TryGetValue(receiverId, out var receiverConnections))
            {
                foreach (var connId in receiverConnections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage", new
                    {
                        sentMessage.Id,
                        SenderId = sentMessage.Sender.Id,
                        SenderUsername = sentMessage.Sender.Username,
                        Content = sentMessage.Content,
                        SentAt = sentMessage.SentAt,
                        IsRead = sentMessage.IsRead
                    });
                }
            }
            // Подтверждаем отправителю
            await Clients.Caller.SendAsync("MessageSent", new
            {
                sentMessage.Id,
                ReceiverId = sentMessage.Receiver.Id,
                ReceiverUsername = sentMessage.Receiver.Username,
                Content = sentMessage.Content,
                SentAt = sentMessage.SentAt
            });
            Console.WriteLine($"[Сервер] Сообщение от {senderId} к {receiverId} отправлено. Content: {message}");
        }

        public async Task GetConversation(int partnerId, int skip = 0, int take = 50)
        {
            Console.WriteLine($"[Сервер] Метод GetConversation вызван. ConnectionId: {Context.ConnectionId}, partnerId: {partnerId}, skip: {skip}, take: {take}");
            Console.WriteLine($"[Сервер] Количество соединений в _connectionUserMap: {_connectionUserMap.Count}");

            // Выводим все connectionId из словаря для отладки
            foreach (var kvp in _connectionUserMap)
            {
                Console.WriteLine($"[Сервер] Словарь содержит: ConnectionId='{kvp.Key}', UserId={kvp.Value}");
            }

            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] ОШИБКА: ConnectionId '{Context.ConnectionId}' НЕ НАЙДЕН в _connectionUserMap");
                await Clients.Caller.SendAsync("ConversationLoadFailed", "Вы не авторизованы или ваше соединение устарело. Пожалуйста, войдите снова.");
                return;
            }

            Console.WriteLine($"[Сервер] Найден userId: {userId} для ConnectionId: {Context.ConnectionId}");
            Console.WriteLine($"[Сервер] Пользователь ID '{userId}' запрашивает переписку с partnerId: {partnerId}");

            try
            {
                var messages = await _chatService.GetConversationAsync(userId, partnerId, skip, take);
                Console.WriteLine($"[Сервер] Получено {messages.Count} сообщений из базы данных для диалога {userId}-{partnerId}.");

                var messageData = messages.Select(m => new
                {
                    m.Id,
                    SenderId = m.Sender?.Id, // Используем безопасный доступ
                    SenderUsername = m.Sender?.Username, // Используем безопасный доступ
                    ReceiverId = m.Receiver?.Id, // Используем безопасный доступ
                    ReceiverUsername = m.Receiver?.Username, // Используем безопасный доступ
                    m.Content,
                    m.SentAt,
                    m.IsRead
                }).ToList();

                Console.WriteLine($"[Сервер] Отправка {messageData.Count} сообщений клиенту (ConversationLoaded).");
                await Clients.Caller.SendAsync("ConversationLoaded", partnerId, messageData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Сервер] Ошибка при загрузке переписки в GetConversation для пользователя {userId} и партнера {partnerId}: {ex.Message}");
                Console.WriteLine($"[Сервер] StackTrace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("ConversationLoadFailed", $"Ошибка загрузки переписки: {ex.Message}");
            }
        }

        public async Task MarkMessageAsRead(int messageId)
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] MarkMessageAsRead: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} помечает сообщение {messageId} как прочитанное.");
            await _chatService.MarkMessageAsReadAsync(messageId, userId);
        }

        public async Task GetUserList()
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] GetUserList: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} запрашивает список пользователей.");
            var users = await _userService.GetAllUsersAsync();
            var userList = users.Where(u => u.Id != userId).Select(u => new
            {
                u.Id,
                u.Username,
                u.IsOnline,
                u.LastSeen
            }).ToList();

            await Clients.Caller.SendAsync("UserListLoaded", userList);

            // После загрузки списка пользователей
            foreach (var user in users)
            {
                if (user.Id != userId)
                    await _userService.AddNotificationSubscriptionAsync(userId, user.Id);
            }
        }

        public async Task AddNotificationSubscription(int targetUserId)
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] AddNotificationSubscription: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} добавляет подписку на уведомления для {targetUserId}.");
            var success = await _userService.AddNotificationSubscriptionAsync(userId, targetUserId);

            // Добавляем ConnectionId в группу targetUserId для получения уведомлений
            if (success)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
            }

            await Clients.Caller.SendAsync("NotificationSubscriptionAdded", targetUserId, success);
        }

        public async Task RemoveNotificationSubscription(int targetUserId)
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] RemoveNotificationSubscription: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} удаляет подписку на уведомления для {targetUserId}.");
            var success = await _userService.RemoveNotificationSubscriptionAsync(userId, targetUserId);

            // Удаляем ConnectionId из группы targetUserId
            if (success)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{targetUserId}");
            }

            await Clients.Caller.SendAsync("NotificationSubscriptionRemoved", targetUserId, success);
        }

        public async Task GetNotificationSubscriptions()
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] GetNotificationSubscriptions: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} запрашивает подписки на уведомления.");
            var subscriptions = await _userService.GetSubscriptionsAsync(userId);
            var subscriptionData = subscriptions.Select(s => new
            {
                s.Id,
                s.Username,
                s.IsOnline,
                s.LastSeen
            }).ToList();

            await Clients.Caller.SendAsync("NotificationSubscriptionsLoaded", subscriptionData);
        }

        public async Task GetConversationPartners()
        {
            if (!_connectionUserMap.TryGetValue(Context.ConnectionId, out int userId))
            {
                Console.WriteLine($"[Сервер] GetConversationPartners: ConnectionId '{Context.ConnectionId}' не найден в _connectionUserMap. Отклонено.");
                return;
            }
            Console.WriteLine($"[Сервер] Пользователь {userId} запрашивает партнеров по переписке.");
            var partners = await _chatService.GetConversationPartnersAsync(userId);
            var partnersData = partners.Select(p => new
            {
                p.Id,
                p.Username,
                p.IsOnline,
                p.LastSeen
            }).ToList();

            await Clients.Caller.SendAsync("ConversationPartnersLoaded", partnersData);
        }
    }
}
