using EdPr.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdPr.ViewModels
{
    public class EmployeeDashboardViewModel : ViewModelBase
    {
        private readonly EdPrEntities _db;
        private readonly UserService _userService;
        private string _oldLogin;

        public UserProfileViewModel UserProfileVM { get; }
        public ICommand SaveCommand { get; }

        public EmployeeDashboardViewModel(EdPrEntities db, User currentUser)
        {
            _db = db;
            _userService = new UserService(db);
            _oldLogin = currentUser.Login;

            UserProfileVM = new UserProfileViewModel(_userService, currentUser);
            SaveCommand = new RelayCommand(_ => ExecuteSave());
        }

        private void ExecuteSave()
        {
            if (!_userService.ValidateCommonFields(UserProfileVM.User, UserProfileVM.Password, _oldLogin, out string err))
            {
                MessageBox.Show(err); return;
            }

            _userService.PrepareUserAccount(UserProfileVM.User, UserProfileVM.Password, _oldLogin);
            _db.SaveChanges();
            UserProfileVM.Password = string.Empty;
            _oldLogin = UserProfileVM.User.Login;
            MessageBox.Show("Данные успешно обновлены!");
        }
    }
}
