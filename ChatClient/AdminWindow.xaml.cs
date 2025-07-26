using System.Windows;
using System.Windows.Input;
using ChatClient.ViewModels;

namespace ChatClient
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            // Устанавливаем DataContext, если он не установлен в XAML
            // this.DataContext = new AdminViewModel(); 
            // Предполагается, что DataContext установлен в XAML: <Window.DataContext><vm:AdminViewModel/></Window.DataContext>
        }

        // Обработчики для перетаскивания окна
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Обработчики для кнопок управления окном
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрываем только AdminWindow
        }
    }
}
