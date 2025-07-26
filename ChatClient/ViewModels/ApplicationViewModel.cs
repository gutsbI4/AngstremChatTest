using System;
using System.Threading.Tasks;
using System.Windows; // Для MessageBox
using ChatClient.Base;
using ChatClient.Models;
using ChatClient.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using Microsoft.Extensions.Logging; // Для обработки JsonElement

namespace ChatClient.ViewModels
{
    // Главный ViewModel, управляющий текущим состоянием приложения (логин/регистрация/чат)
    public class ApplicationViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        private Models.User? _loggedInUser;

        private readonly HubConnection _hubConnection;
        private readonly ChatService _chatService;

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public Models.User? LoggedInUser
        {
            get => _loggedInUser;
            set => SetProperty(ref _loggedInUser, value);
        }

        public ApplicationViewModel()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:3001/chathub")
                .WithAutomaticReconnect()
                .Build();

            _chatService = new ChatService();

            SetupSignalRConnectionHandlers();
            SetupLoginSuccessHandler();

            _currentViewModel = new LoginViewModel(_hubConnection, _chatService, OnLoginSuccess, NavigateToRegister);

            _ = StartHubConnectionAsync();
        }

        private async Task StartHubConnectionAsync()
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    Console.WriteLine("SignalR Connection Started.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
                ShowNotification("Ошибка подключения", "Не удалось подключиться к чат-серверу. Пожалуйста, проверьте подключение и повторите попытку.");
            }
        }

        private void SetupSignalRConnectionHandlers()
        {
            _hubConnection.Closed += async (error) =>
            {
                Console.WriteLine($"SignalR Connection Closed: {error?.Message}");
                // Попытка переподключения
                await Task.Delay(TimeSpan.FromSeconds(5)); // Задержка перед попыткой переподключения
                await StartHubConnectionAsync();
            };

            _hubConnection.Reconnecting += (error) =>
            {
                Console.WriteLine($"SignalR Connection Reconnecting: {error?.Message}");
                ShowNotification("Подключение к серверу", "Потеряно соединение с чат-сервером. Попытка переподключения...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"SignalR Connection Reconnected. Connection ID: {connectionId}");
                ShowNotification("Подключение к серверу", "Соединение с чат-сервером восстановлено.");

                if (CurrentViewModel is MainViewModel mainVm && LoggedInUser != null)
                {
                    _ = mainVm.Initialize(LoggedInUser); // Перезагружаем данные для текущего пользователя
                }
                return Task.CompletedTask;
            };
        }

        private void SetupLoginSuccessHandler()
        {
            _hubConnection.On<JsonElement>("LoginSuccess", (userData) =>
            {
                App.Current.Dispatcher.Invoke(async () =>
                {
                    var user = JsonSerializer.Deserialize<Models.User>(userData.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (user != null)
                    {
                        LoggedInUser = user;

                        var mainVm = new MainViewModel(_hubConnection, _chatService, OpenAdminPanel, ShowNotification);
                        await mainVm.Initialize(user);
                        CurrentViewModel = mainVm;
                    }
                    else
                    {
                        ShowNotification("Ошибка входа", "Не удалось получить данные пользователя после входа.");
                        NavigateToLogin();
                    }
                });
            });

            _hubConnection.On<string>("LoginFailed", (errorMessage) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (CurrentViewModel is LoginViewModel loginVm)
                    {
                        loginVm.ErrorMessage = errorMessage;
                    }
                    ShowNotification("Ошибка входа", errorMessage);
                });
            });
        }

        private void OnLoginSuccess(Models.User user)
        {
            // Этот метод может быть пустым или использоваться для дополнительной логики,
            // так как основной переход на MainViewModel теперь управляется через SignalR "LoginSuccess"
        }

        private void NavigateToRegister()
        {
            CurrentViewModel = new RegistrationViewModel(_chatService, NavigateToLogin);
        }

        private void NavigateToLogin()
        {
            CurrentViewModel = new LoginViewModel(_hubConnection, _chatService, OnLoginSuccess, NavigateToRegister);
        }

        private void OpenAdminPanel()
        {
            // Создаем и показываем AdminWindow
            var adminWindow = new AdminWindow();
            adminWindow.Show();
        }

        internal void ShowNotification(string title, string message)
        {
            // Простое уведомление через MessageBox. Можно улучшить до Toast-уведомлений.
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async void Shutdown()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            _chatService?.Dispose();
        }
    }
}
