using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EdPr.ViewModels
{
    public class CatalogViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private User _user;
        private ObservableCollection<GoodsTableItem> _goods;
        private decimal? _cartTotal;

        private int? _currentCategoryId;

        public ObservableCollection<GoodsTableItem> Goods
        {
            get => _goods;
            set
            {
                _goods = value;
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

        public ICommand AddGoodToCartCommand { get; }


        public CatalogViewModel(EdPrEntities db, User user, GoodCategory category = null)
        {
            _db = db;
            _user = user;

            Goods = LoadGoodsAndMatchWithCart(category?.Id);

            UpdateTotal();

            AddGoodToCartCommand = new RelayCommand(AddSelectedGoodToCart);
        }

        private ObservableCollection<GoodsTableItem> LoadGoodsAndMatchWithCart(int? categoryId)
        {
            _currentCategoryId = categoryId;
            var itemsList = new List<GoodsTableItem>();

            if (categoryId == null)
            {
                var userCart = _db.GoodsCarts
                    .Where(x => x.CartId == _user.Id)
                    .ToList();

                foreach (var cartRecord in userCart)
                {
                    var item = new GoodsTableItem
                    {
                        Good = cartRecord.Good,
                        IsSelected = true,
                        Quantity = (int)cartRecord.Quantitty
                    };

                    item.PropertyChanged += OnItemPropertyChanged;
                    itemsList.Add(item);
                }
            }
            else
            {
                var categoryGoods = _db.Goods
                    .Where(x => x.CategoryId == categoryId)
                    .ToList();

                var cartItems = _db.GoodsCarts
                    .Where(x => x.CartId == _user.Id && x.Good.CategoryId == categoryId)
                    .ToList();

                foreach (var dbGood in categoryGoods)
                {
                    var item = new GoodsTableItem { Good = dbGood };

                    var cartItem = cartItems.FirstOrDefault(x => x.GoodId == dbGood.Id);
                    if (cartItem != null)
                    {
                        item.IsSelected = true;
                        item.Quantity = (int)cartItem.Quantitty;
                    }
                    else
                    {
                        item.IsSelected = false;
                        item.Quantity = 1;
                    }

                    item.PropertyChanged += OnItemPropertyChanged;
                    itemsList.Add(item);
                }
            }

            return new ObservableCollection<GoodsTableItem>(itemsList);
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is GoodsTableItem item)
            {
                if (e.PropertyName == nameof(GoodsTableItem.IsSelected))
                {
                    if (item.IsSelected)
                    {
                        AddGoodToCart(item);
                    }
                    else
                    {
                        RemoveGoodFromCart(item);

                        if (_currentCategoryId == null)
                        {
                            Goods.Remove(item);
                        }
                    }
                    UpdateTotal();
                }
                else if (e.PropertyName == nameof(GoodsTableItem.Quantity))
                {
                    if (item.IsSelected)
                    {
                        UpdateGoodQuantity(item);
                        UpdateTotal();
                    }
                }
            }
        }

        private void AddGoodToCart(GoodsTableItem item)
        {
            try
            {
                var existing = _db.GoodsCarts.FirstOrDefault(x => x.CartId == _user.Id && x.GoodId == item.Good.Id);
                if (existing == null)
                {
                    var cartItem = new GoodsCart()
                    {
                        GoodId = item.Good.Id,
                        Quantitty = item.Quantity,
                        CartId = _user.Id
                    };
                    _db.GoodsCarts.Add(cartItem);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в корзину: {ex.Message}");
            }
        }

        private void RemoveGoodFromCart(GoodsTableItem item)
        {
            try
            {
                var existing = _db.GoodsCarts.FirstOrDefault(x => x.CartId == _user.Id && x.GoodId == item.Good.Id);
                if (existing != null)
                {
                    _db.GoodsCarts.Remove(existing);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из корзины: {ex.Message}");
            }
        }

        private void UpdateGoodQuantity(GoodsTableItem item)
        {
            try
            {
                var existing = _db.GoodsCarts.FirstOrDefault(x => x.CartId == _user.Id && x.GoodId == item.Good.Id);
                if (existing != null)
                {
                    existing.Quantitty = item.Quantity;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении количества: {ex.Message}");
            }
        }

        private void UpdateTotal()
        {
            var total = _db.GoodsCarts
                .Where(x => x.CartId == _user.Id)
                .Sum(x => (decimal?)(x.Quantitty * x.Good.Price));

            CartTotal = total ?? 0;
        }

        private void AddSelectedGoodToCart(object parameter)
        {
            if (parameter is GoodsTableItem selectedGood)
            {
                selectedGood.IsSelected = true;
            }
        }

        public class GoodsTableItem : ViewModelBase
        {
            private Good _good;
            private byte[] _image;
            private bool _isSelected;
            private int _quantity = 1;

            public Good Good
            {
                get => _good;
                set
                {
                    _good = value;
                    _image = value.Image;
                    OnPropertyChanged();
                }
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }

            public int Quantity
            {
                get => _quantity;
                set
                {
                    if (value == 0 && IsSelected)
                    {
                        if (AskDeleteItem())
                        {
                            IsSelected = false;
                        }
                        return;
                    }

                    _quantity = value;
                    OnPropertyChanged();
                }
            }

            public BitmapImage ImageSource
            {
                get
                {
                    if (_image == null || _image.Length == 0)
                    {
                        var stubUri = new Uri("pack://application:,,,/Imgs/No_photo.png");
                        return new BitmapImage(stubUri);
                    }

                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(_image))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();
                    return bitmap;
                }
            }

            public bool AskDeleteItem()
            {
                MessageBoxResult result =
                    MessageBox.Show("Удалить товар из корзины?", "Удаление",
                    MessageBoxButton.YesNo);

                return result == MessageBoxResult.Yes;
            }
        }
    }
}