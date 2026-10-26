using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Documents;

namespace EdPr.Services
{
    public class UserService
    {
        private EdPrEntities _db;

        private DateTime _startDate = DateTime.Now.AddYears(-100);
        private DateTime _endDate = DateTime.Now.AddYears(-14);

        public UserService(EdPrEntities db) => _db = db;

        public bool ValidatePersonalDataFields(User user, out string error, bool isAddressRequired = false)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(user.Surname) || string.IsNullOrWhiteSpace(user.Name)
                || user.Gender == null || string.IsNullOrWhiteSpace(user.Address?.Street))
            {
                error = isAddressRequired ? "Заполните все поля, адрес (не считая улицы) и отчество могут быть пустыми"
                    : "Заполните все обязательные поля. Подъезд и этаж могут быть пустыми";
                return false;
            }

            if (isAddressRequired)
            {
                if (string.IsNullOrWhiteSpace(user.Address?.House)
                || user.Address?.House.Length > 2
                || user.Address?.Room <= 0 || user.Address.Street.Length > 50
                || string.IsNullOrWhiteSpace(user.Address?.Street))
                {
                    error = "Заполните адрес, подъезд и этаж необязательны";
                    return false;
                }
            }

                if (!Regex.IsMatch(user.Email ?? "", @"^[a-zA-Z0-9_.-]+@[a-zA-Z0-9._-]+\.[a-zA-Z]+$"))
            {
                error = "Неверный формат почты (**@**.**)";
                return false;
            }

            if (!Regex.IsMatch(user.Passport ?? "", @"^\d{4}\s?\d{6}$"))
            {
                error = "Неверный формат паспорта (10 цифр. Можно через пробел, 4 + 6)";
                return false;
            }

            string phone = Regex.Replace(user.Phone, @"[\D]", "");
            if (!Regex.IsMatch(phone ?? "", @"^[78]\d{10}$"))
            {
                error = "Неверный формат номера (70000000000)";
                return false;
            }

            if (user.Birthday < _startDate || user.Birthday > _endDate)
            {
                error = "Укажите подходящий возраст (14-100)";
                return false;
            }

            return true;
        }

        public bool ValidateCommonFields(User user, string password, string oldLogin, out string error)
        {
            error = null;

            if (!ValidatePersonalDataFields(user, out error))
            {
                return false;
            }

            if (!Regex.IsMatch(user.Login ?? "", @"^[a-zA-Z0-9_-]+$"))
            {
                error = "Логин должен состоять из латинских букв и цифр";
                return false;
            }

            if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(user.Account?.Password))
            {
                // Пароль не меняется — проверка формата не нужна.
            }
            else if (!Regex.IsMatch(password ?? "", @"^(?=.*[0-9])[a-zA-Z0-9!@#$%^&*_-]{8,}$"))
            {
                error = "Пароль должен содержать латинские буквы и хотя бы 1 цифру. Мин длина 8 символов";
                return false;
            }

            user.Passport = Regex.Replace(user.Passport, " ", "");

            if (oldLogin != user.Login && _db.Accounts.Any(x => x.Login == user.Login))
            {
                error = "Этот логин уже занят";
                return false;
            }

            return true;
        }

        public void PrepareUserAccount(User user, string plainPassword, string oldLogin)
        {
            if (oldLogin != user.Login)
            {
                string passwordToStore = string.IsNullOrWhiteSpace(plainPassword)
                    && !string.IsNullOrEmpty(user.Account?.Password)
                    ? user.Account.Password
                    : GetHashPassword(plainPassword);

                var account = new Account
                {
                    Login = user.Login,
                    Password = passwordToStore
                };

                _db.Accounts.Add(account);
                user.Account = account;
            }
            else if (!string.IsNullOrWhiteSpace(plainPassword))
            {
                user.Account.Password = GetHashPassword(plainPassword);
            }
        }

        public void EnsureUserCart(User user)
        {
            if (user.UserCart == null)
            {
                user.UserCart = new UserCart();
            }
        }

        public string GetHashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                byte[] hashBytes = new byte[48];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 32);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public byte[] SelectPhoto()
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Изображения (*.jpg;*.jpeg;*.png) | *.jpg;*.jpeg;*png" };
            return dialog.ShowDialog() == true ? File.ReadAllBytes(dialog.FileName) : null;
        }

        public BitmapImage LoadPhoto(byte[] imgBytes)
        {
            if (imgBytes == null || imgBytes.Length == 0) return null;
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imgBytes))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }
    }
}
