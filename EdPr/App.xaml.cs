using EdPr.ViewModels;
using EdPr.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EdPr
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Не закрывает приложение при закрытии первого окна. Без этого после авторизации ничего не будет
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var db = new EdPrEntities();

            var authViewModel = new AuthViewModel(db);
            var authWindow = new AuthWindow();
            authWindow.DataContext = authViewModel;
            
            if (authWindow.ShowDialog() == true)
            {
                var mainWindow = new MainWindow();
                mainWindow.DataContext = new MainViewModel(db, authViewModel?.User);

                // Теперь при закрытии главного окна, программа закроется
                Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}
