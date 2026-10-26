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
    public class UserRegistrationViewModel : ViewModelBase
    {
        private readonly EdPrEntities _db;
        private readonly UserService _userService;

        public UserProfileViewModel UserProfileVM { get; }

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public UserRegistrationViewModel(EdPrEntities db)
        {
            _db = db;
            _userService = new UserService(db);

            var newUser = new User { Account = new Account() };
            UserProfileVM = new UserProfileViewModel(_userService, newUser);
            RegisterCommand = new RelayCommand(_ => ExecuteRegister());
        }

        private void ExecuteRegister()
        {
            if (!_userService.ValidateCommonFields(UserProfileVM.User, UserProfileVM.Password, "", out string err))
            {
                MessageBox.Show(err); return;
            }

            var user = UserProfileVM.User;

            _userService.PrepareUserAccount(user, UserProfileVM.Password, "");
            _userService.EnsureUserCart(user);

            _db.Users.Add(user);
            _db.SaveChanges();
            MessageBox.Show("Вы успешно зарегистрировались!");

            user = null;
        }

        private void ExecuteLogin()
        {

        }
    }
}
