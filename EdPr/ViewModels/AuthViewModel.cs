using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdPr.ViewModels

{
    public class AuthViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private string _login;
        private string _password;

        public User User { get; set; }
        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand GoToRegistrationCommand { get; }

        // Оказывается, обязательно нужен пусто конструктор, иначе программа не запуститься =) (Только если есть в xaml привязка
        // DataContext. Но в таком случае вызываться будет только он, просто конструктор с параметром можно забыть)

        public AuthViewModel(EdPrEntities db)
        {
            _db = db;
            LoginCommand = new RelayCommand(ExecuteLogin);
            GoToRegistrationCommand = new RelayCommand(ExecuteGoToRegistration);
        }

        private bool CheckPassword(string input, string hashPass)
        {
            byte[] hashBytes = Convert.FromBase64String(hashPass);

            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            using (var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 100_000, HashAlgorithmName.SHA256))
            {
                byte[] hashToCheck = pbkdf2.GetBytes(32);

                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hashToCheck[i])
                        return false;
                }
                return true;
            }
        }

        //private string GetHashPassword(string password)
        //{
        //    byte[] salt = new byte[16];
        //    using (var rng = RandomNumberGenerator.Create())
        //    {
        //        rng.GetBytes(salt);
        //    }

        //    using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
        //    {
        //        byte[] hash = pbkdf2.GetBytes(32);

        //        byte[] hashBytes = new byte[48];
        //        Array.Copy(salt, 0, hashBytes, 0, 16);
        //        Array.Copy(hash, 0, hashBytes, 16, 32);

        //        return Convert.ToBase64String(hashBytes);
        //    }
        //}


        public void ExecuteLogin(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Login)
                || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            User = _db.Users
                .FirstOrDefault(x => x.Login == Login);

            if (User == null)
            {
                MessageBox.Show("Пользователь не найден");
                return;
            }

            //var d = GetHashPassword(Password);

            if (!CheckPassword(Password, User.Account.Password))
            {
                MessageBox.Show("Пароль неверный");
                return;
            }

            // Если передать окно через параметр, оно закроется. Сначала выполниться код в {}
            // Для этого еще в xaml нужно прописать CommandParameter
            if (parameter is Window currentWindow)
            {
                currentWindow.DialogResult = true;


                Log newLog = new Log()
                {
                    User = User,
                    DateTime = DateTime.Now
                };

                try
                {
                    _db.Logs.Add(newLog);
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при записи в журнал входа: {ex.Message}");
                }
            }
        }

        public void ExecuteGoToRegistration(object parametr)
        {
            // Просто выход, в MainWindow будет отстальная логика
            if (parametr is Window currentWindow)
            {
                currentWindow.DialogResult = true;
            }
        }
    }
}
