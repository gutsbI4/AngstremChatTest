using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatClient.Base;
using ChatClient.Models;
using ChatClient.Services;
using System.Linq;

namespace ChatClient.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private UserStatistics _statistics = new UserStatistics();
        private ObservableCollection<User> _users = new ObservableCollection<User>();
        private User? _selectedUser;
        private bool _isLoading;
        private string _errorMessage = string.Empty;

        private readonly AdminService _adminService;

        public UserStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public AdminViewModel()
        {
            _adminService = new AdminService();
            LoadDataCommand = new RelayCommand(async _ => await LoadData());
            DeleteUserCommand = new RelayCommand(async parameter => await DeleteUser(parameter), parameter => CanDeleteUser(parameter));
            _ = LoadData();
        }

        private async Task LoadData()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                Statistics = await _adminService.GetStatisticsAsync();
                var usersList = await _adminService.GetAllUsersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var user in usersList)
                    {
                        Users.Add(user);
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDeleteUser(object? parameter)
        {
            return parameter is User;
        }

        private async Task DeleteUser(object? parameter)
        {
            if (parameter is User userToDelete)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пользователя {userToDelete.Username} (ID: {userToDelete.Id})?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                    try
                    {
                        bool success = await _adminService.DeleteUserAsync(userToDelete.Id);
                        if (success)
                        {
                            MessageBox.Show(
                                $"Пользователь {userToDelete.Username} успешно удален.",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            await LoadData();
                        }
                        else
                        {
                            ErrorMessage = $"Не удалось удалить пользователя {userToDelete.Username}.";
                            MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Ошибка при удалении пользователя: {ex.Message}";
                        MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
