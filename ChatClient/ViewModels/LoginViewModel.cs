using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls; // Добавляем для PasswordBox
using ChatClient.Base;
using ChatClient.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username = string.Empty;
        // Пароль больше не хранится как свойство ViewModel
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        private readonly HubConnection _hubConnection;
        private readonly ChatService _chatService;
        private readonly Action<Models.User> _onLoginSuccess;
        private readonly Action _onNavigateToRegister;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }


        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }

        public LoginViewModel(HubConnection hubConnection, ChatService chatService, Action<Models.User> onLoginSuccess, Action onNavigateToRegister)
        {
            _hubConnection = hubConnection;
            _chatService = chatService;
            _onLoginSuccess = onLoginSuccess;
            _onNavigateToRegister = onNavigateToRegister;

            LoginCommand = new RelayCommand(async parameter => await ExecuteLogin(parameter), _ => CanExecuteLogin());
            NavigateToRegisterCommand = new RelayCommand(_ => ExecuteNavigateToRegister());
        }

        private bool CanExecuteLogin() => !IsLoading;

        private async Task ExecuteLogin(object parameter)
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            var passwordBox = parameter as PasswordBox;
            string password = passwordBox?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Заполните все поля";
                IsLoading = false;
                return;
            }

            try
            {
                // Устанавливаем соединение SignalR, если оно еще не установлено
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await _hubConnection.StartAsync();
                }

                var success = await _hubConnection.InvokeAsync<bool>("Login", Username, password);

                if (success)
                {
                    // Если логин успешен, SignalR хаб отправит событие "LoginSuccess"
                    // Обработка этого события будет в MainViewModel
                    // Здесь мы просто ждем, что хаб отправит данные пользователя
                }
                else
                {
                    ErrorMessage = "Неверные данные для входа";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                if (passwordBox != null)
                {
                    passwordBox.Password = string.Empty;
                }
            }
        }

        private void ExecuteNavigateToRegister()
        {
            _onNavigateToRegister?.Invoke();
        }
    }
}
