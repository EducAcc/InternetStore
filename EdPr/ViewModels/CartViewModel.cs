using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdPr.ViewModels
{
    public class CartViewModel : ViewModelBase
    {
        private CatalogViewModel _goodsControl;
        private EdPrEntities _db;
        private User _user;

        public CatalogViewModel GoodsControl
        {
            get => _goodsControl;
            set
            {
                _goodsControl = value;
                OnPropertyChanged();
            }
        }

        public ICommand BackCommand { get; }
        public ICommand GoRequestViewCommand { get; }

        public event Action BackRequested;
        public event Action<RequestViewModel> OnOpenRequestViewRequested;

        public CartViewModel(EdPrEntities db, User user)
        {
            _db = db;
            _user = user;

            GoodsControl = new CatalogViewModel(db, user);

            BackCommand = new RelayCommand(_ => BackRequested?.Invoke());
            GoRequestViewCommand = new RelayCommand(ExecuteGoRequestView);
        }

        public void ExecuteGoRequestView(object parameter)
        {
            if (GoodsControl.Goods.Count <= 0)
            {
                MessageBox.Show("Корзина пуста");
            }
            else
            {
                var requestVM = new RequestViewModel(_db, _user);

                OnOpenRequestViewRequested?.Invoke(requestVM);
            }
        }
    }
}
