using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatClient.ViewModels;

namespace ChatClient
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPassword =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPassword);

        public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPassword, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if (!GetIsUpdating(passwordBox))
                {
                    passwordBox.Password = (string)e.NewValue;
                }
                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetIsUpdating(passwordBox, true);
                SetBoundPassword(passwordBox, passwordBox.Password);
                SetIsUpdating(passwordBox, false);
            }
        }

        private static readonly DependencyProperty IsUpdating =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxHelper));

        private static bool GetIsUpdating(DependencyObject d) => (bool)d.GetValue(IsUpdating);

        private static void SetIsUpdating(DependencyObject d, bool value) => d.SetValue(IsUpdating, value);
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closed += (s, e) => (DataContext as ApplicationViewModel)?.Shutdown();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox &&
                DataContext is ApplicationViewModel appVm &&
                appVm.CurrentViewModel is MainViewModel mainVm &&
                mainVm.IsSearchUsersPlaceholder)
            {
                textBox.Text = "";
                mainVm.SearchUsersText = "";
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox &&
                DataContext is ApplicationViewModel appVm &&
                appVm.CurrentViewModel is MainViewModel mainVm &&
                string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Поиск пользователей...";
                mainVm.SearchUsersText = "Поиск пользователей...";
            }
        }

        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox &&
                DataContext is ApplicationViewModel appVm &&
                appVm.CurrentViewModel is MainViewModel mainVm &&
                mainVm.IsMessageInputPlaceholder)
            {
                textBox.Text = "";
                mainVm.MessageInputText = "";
            }
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox &&
                DataContext is ApplicationViewModel appVm &&
                appVm.CurrentViewModel is MainViewModel mainVm &&
                string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Введите сообщение...";
                mainVm.MessageInputText = "Введите сообщение...";
            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                DataContext is ApplicationViewModel appVm &&
                appVm.CurrentViewModel is MainViewModel mainVm)
            {
                mainVm.SendMessageCommand.Execute(null);
            }
        }
    }
}
