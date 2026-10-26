using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdPr.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentMenu;
        
        public ViewModelBase CurrentMenu
        {
            get => _currentMenu;
            set
            {
                _currentMenu = value;
                OnPropertyChanged();
            }
        }

        // Вызывается из App.cs, там тоже код есть
        public MainViewModel(EdPrEntities db, User user)
        {
            if (user == null)
            {
                CurrentMenu = new UserRegistrationViewModel(db);
                return;
            }

            if (user.Occupation == 1)
            {
                CurrentMenu = new AdminMenuViewModel(db, user);
            }
            else if (user.Occupation == null)
            {
                CurrentMenu = new StoreMainViewModel(db, user);
            }
            else
            {
                CurrentMenu = new EmployeeDashboardViewModel(db, user);
            }


        }
    }
}
