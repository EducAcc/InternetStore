using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EdPr.ViewModels
{
    public class StoreMainViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private User _user;
        private ViewModelBase _currentControl;
        private CategoriesViewModel _categoriesVM;

        public ViewModelBase CurrentControl
        {
            get => _currentControl;
            set
            {
                _currentControl = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand GoToVKCommand { get; }

        public StoreMainViewModel(EdPrEntities db, User user)
        {
            _db = db;
            _user = user;
            _categoriesVM = new CategoriesViewModel(_db, _user);

            GoToVKCommand = new RelayCommand(ExecuteGoToVK);

            _categoriesVM.OpenCartRequested += OnOpenCartRequested;

            CurrentControl = _categoriesVM;
        }

        private void ExecuteGoToVK(object parameter)
        {
            Process.Start("https://m.vk.com");
        }


        private void OnOpenCartRequested(CartViewModel cartVM)
        {
            cartVM.BackRequested += () =>
            {
                CurrentControl = _categoriesVM;
            };

            CurrentControl = cartVM;

            cartVM.OnOpenRequestViewRequested += OnOpenRequestView;
        }

        private void OnOpenRequestView(RequestViewModel requestVM)
        {
            requestVM.BackRequested += () =>
            {
                CurrentControl = _categoriesVM;
            };

            CurrentControl = requestVM;
        }
    }
}
