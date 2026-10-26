using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static EdPr.ViewModels.CatalogViewModel;

namespace EdPr.ViewModels
{
    public class CategoriesViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private User _user;
        private CatalogViewModel _goodControl;
        private GoodCategory _selectedCategory;
        private List<GoodCategory> _categories;
        private decimal? _cartTotal;

        public CatalogViewModel GoodControl
        {
            get => _goodControl;
            set
            {
                _goodControl = value;
                OnPropertyChanged();
            }
        }
        public GoodCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();

                OnCategorySelectionChanged();
            }
        }
        public List<GoodCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public decimal? CartTotal
        {
            get => _cartTotal;
            set
            {
                _cartTotal = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenCartDialogCommand { get; }

        public event Action<CartViewModel> OpenCartRequested;

        public CategoriesViewModel(EdPrEntities db, User user)
        {
            _db = db;
            _user = user;

            Categories = _db.GoodCategories.ToList();

            OpenCartDialogCommand = new RelayCommand(ExecuteOpenCartDialog);
        }


        private void ExecuteOpenCartDialog(object parameter)
        {
            var cartDialog = new CartViewModel(_db, _user);


            // Вызов события для всех подписанных
            OpenCartRequested?.Invoke(cartDialog);
        }

        private void OnCategorySelectionChanged()
        {
            if (SelectedCategory != null)
            {
                var goodControl = new CatalogViewModel(_db, _user, SelectedCategory);

                GoodControl = goodControl;

                CartTotal = goodControl.CartTotal;
            }
        }

    }
}
