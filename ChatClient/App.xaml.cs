using System.Windows;
using ChatClient.ViewModels;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow mainWindow = new MainWindow();
            // DataContext для MainWindow уже устанавливается в XAML на ApplicationViewModel
            // Но мы можем получить к нему доступ, если нужно
            // var appViewModel = mainWindow.DataContext as ApplicationViewModel;

            mainWindow.Show();
        }
    }
}