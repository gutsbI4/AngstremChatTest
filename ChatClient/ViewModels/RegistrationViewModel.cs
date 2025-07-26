using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls; // Добавляем для PasswordBox
using ChatClient.Base;
using ChatClient.Services;

namespace ChatClient.ViewModels
{
    // ViewModel для окна регистрации
    public class RegistrationViewModel : BaseViewModel
    {
        private string _username = string.Empty;
        private string _email = string.Empty;
        // Пароль больше не хранится как свойство ViewModel
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        private readonly ChatService _chatService;
        private readonly Action _onNavigateToLogin;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        // Удалено свойство Password, так как оно будет передаваться напрямую из View

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

        public ICommand RegisterCommand { get; }
        public ICommand NavigateToLoginCommand { get; }

        public RegistrationViewModel(ChatService chatService, Action onNavigateToLogin)
        {
            _chatService = chatService;
            _onNavigateToLogin = onNavigateToLogin;

            // Команда RegisterCommand теперь принимает PasswordBox как параметр
            RegisterCommand = new RelayCommand(async parameter => await ExecuteRegister(parameter), _ => CanExecuteRegister());
            NavigateToLoginCommand = new RelayCommand(_ => ExecuteNavigateToLogin());
        }

        private bool CanExecuteRegister() => !IsLoading;

        // Метод ExecuteRegister теперь принимает объект (PasswordBox)
        private async Task ExecuteRegister(object parameter)
        {
            ErrorMessage = string.Empty;
            IsLoading = true;

            var passwordBox = parameter as PasswordBox;
            string password = passwordBox?.Password ?? string.Empty; // Получаем пароль напрямую

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Заполните все поля";
                IsLoading = false;
                return;
            }

            try
            {
                var success = await _chatService.RegisterUserAsync(Username, Email, password);
                if (success)
                {
                    System.Windows.MessageBox.Show("Регистрация успешна! Теперь войдите в систему.", "Успех", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    ExecuteNavigateToLogin();
                }
                else
                {
                    ErrorMessage = "Ошибка регистрации: имя пользователя или email уже существует.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                // Очищаем пароль после использования для безопасности
                if (passwordBox != null)
                {
                    passwordBox.Password = string.Empty;
                }
            }
        }

        private void ExecuteNavigateToLogin()
        {
            _onNavigateToLogin?.Invoke();
        }
    }
}
