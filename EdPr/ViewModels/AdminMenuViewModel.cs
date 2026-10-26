using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.RightsManagement;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace EdPr.ViewModels
{
    public class AdminMenuViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private User _user;
        private List<Log> _logs;
        private ObservableCollection<User> _users;
        private List<Log> _allLogs;
        private ObservableCollection<Good> _goods;
        private ObservableCollection<GoodCategory> _categories;

        private ViewModelBase _sideWindow;
        private User _selectedEmployee;
        private Good _selectedGood = new Good();
        private User _fillteredEmployee;
        private string _employeeSearchInput;
        private DateTime? _startDate;
        private DateTime? _endDate;

        public User User
        {
            get => _user;
            set { _user = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Good> Goods
        {
            get => _goods;
            set { _goods = value; OnPropertyChanged(); }
        }
        public ObservableCollection<GoodCategory> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public ViewModelBase SideWindow
        {
            get => _sideWindow;
            set { _sideWindow = value; OnPropertyChanged(); }
        }

        public User SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (_selectedEmployee != value)
                {
                    _selectedEmployee = value;
                    OnPropertyChanged();

                    if (_selectedEmployee == null) return;

                    OnSelectedEmployeeTableItemChanged(_selectedEmployee);
                }
            }
        }

        public Good SelectedGood
        {
            get => _selectedGood;
            set
            {
                _selectedGood = value;
                OnPropertyChanged();
            }
        }

        public string NewCategoryName { get; set; }

        public ObservableCollection<User> Users
        {
            get => _users;
            set { _users = value; OnPropertyChanged(); }
        }

        public List<Log> Logs
        {
            get => _logs;
            set { _logs = value; OnPropertyChanged(); }
        }

        public string EmployeeSearchInput
        {
            get => _employeeSearchInput;
            set
            {
                _employeeSearchInput = value;
                Logs = GetEmployersLogByString(value);
                OnPropertyChanged();
            }
        }

        public User FilteredEmployee
        {
            get => _fillteredEmployee;
            set
            {
                _fillteredEmployee = value;
                OnPropertyChanged();
                OnSelectedFillterEmployeeItemChanged(_fillteredEmployee);
            }
        }

        public DateTime StartDateBorder { get; } = DateTime.Now.AddMonths(-6).Date;
        public DateTime EndDateBorder { get; } = DateTime.Now.Date;

        public DateTime? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); OnDatePickerItemChanged(value, EndDate); }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); OnDatePickerItemChanged(StartDate, value); }
        }


        public ICommand VipeFilterCommand { get; }
        public ICommand OpenAddEmployeeCommand { get; }
        public ICommand CreateCategoryCommand { get; }
        public ICommand SaveGoodCommand { get; }
        public ICommand AddGoodCommand { get; }
        

        public AdminMenuViewModel(EdPrEntities db, User user)
        {
            _db = db;
            User = user;

            Users = new ObservableCollection<User>(_db.Users.ToList());
            Logs = _db.Logs.ToList();
            Goods = new ObservableCollection<Good>(_db.Goods.ToList());
            _allLogs = _db.Logs.ToList();

            UpdateCategories();

            VipeFilterCommand = new RelayCommand(_ => ExecuteVipeFilter());
            OpenAddEmployeeCommand = new RelayCommand(_ => OpenAddEmployeeForm());
            CreateCategoryCommand = new RelayCommand(_ => ExecuteCreateCategory());
            AddGoodCommand = new RelayCommand(_ => ExecuteAddGood());
            SaveGoodCommand = new RelayCommand(_ => ExecuteSaveGood());

            OpenAddEmployeeForm();
        }


        private void OpenAddEmployeeForm()
        {
            SideWindow = null;
            var addVm = new AdminAddEmployeeViewModel(_db);
            addVm.EmployeeSaved += OnEmployeeSaved;
            SideWindow = addVm;
        }

        private void OnEmployeeSaved(bool isNewUser)
        {
            SelectedEmployee = null;
            Users = new ObservableCollection<User>(_db.Users.ToList());

            if (isNewUser)
                OpenAddEmployeeForm();
        }

        private void OnSelectedEmployeeTableItemChanged(User employee)
        {
            OpenEditEmployeeForm(employee);
        }

        private void OpenEditEmployeeForm(User employee)
        {
            if (employee == null) return;

            SideWindow = null;
            var editVm = new AdminAddEmployeeViewModel(_db, employee);
            editVm.EmployeeSaved += OnEmployeeSaved;
            SideWindow = editVm;
        }

        private void RefreshUsersList()
        {
            SelectedEmployee = null;
            Users = new ObservableCollection<User>(_db.Users.ToList());
            OpenAddEmployeeForm();
        }

        private void UpdateCategories()
        {
            Categories = new ObservableCollection<GoodCategory>(_db.GoodCategories.ToList());
        }

        private bool ValidateGoodFields()
        {
            if (string.IsNullOrWhiteSpace(SelectedGood.Name)
                || SelectedGood.GoodCategory == null
                || SelectedGood.Price == 0
                || SelectedGood.Quantity < 0)
            {
                MessageBox.Show("Заполните все поля корректными данными");
                return false;
            }

            return true;
        } 

        private List<Log> GetEmployersLogByString(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return _allLogs;
            return _allLogs.Where(x => x.User.Login.Contains(input)).ToList();
        }

        private void ExecuteVipeFilter()
        {
            SelectedEmployee = null;
            StartDate = null;
            EndDate = null;
            EmployeeSearchInput = string.Empty;
            Logs = _allLogs;
            OpenAddEmployeeForm();
        }
        private void ExecuteCreateCategory()
        {
            try
            {
                _db.GoodCategories.Add(new GoodCategory
                {
                    Name = NewCategoryName
                });

                _db.SaveChanges();

                MessageBox.Show("Категория добавлена");
                NewCategoryName = null;

                UpdateCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании категории: {ex.Message}");
            }
        }
        private void ExecuteSaveGood()
        {
            if (SelectedGood == null)
            {
                MessageBox.Show("Выберите услугу");
                return;
            }

            if (!ValidateGoodFields()) return;

            try
            {
                _db.SaveChanges();
                MessageBox.Show("Данные сохраненые");

                SelectedGood = new Good();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении данных: {ex.Message}");
            }
        }
        private void ExecuteAddGood()
        {
            if (Goods.Any(x => x.Name == SelectedGood.Name))
            {
                MessageBoxResult result = MessageBox.Show("Товар с таким наименованием уже есть. Добавить?", 
                    "Добавление товара", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            if (!ValidateGoodFields()) return;

            try
            {
                var newGood = new Good
                {
                    Name = SelectedGood.Name,
                    Price = SelectedGood.Price,
                    Quantity = SelectedGood.Quantity,
                    GoodCategory = SelectedGood.GoodCategory
                };

                _db.SaveChanges();
                MessageBox.Show("Данные сохраненые");

                SelectedGood = new Good();
                Goods.Add(newGood);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении товара: {ex.Message}");
            }
        }

        private void OnSelectedFillterEmployeeItemChanged(User employee)
        {
            if (employee == null)
            {
                Users = new ObservableCollection<User>(_db.Users.ToList());
                return;
            }
            Logs = _allLogs.Where(x => x.User.Login == employee.Login).ToList();
        }

        private void OnDatePickerItemChanged(DateTime? startDate, DateTime? endDate)
        {
            if (endDate == null) endDate = EndDateBorder;
            if (startDate == null) startDate = StartDateBorder;
            if (startDate < StartDateBorder || endDate > EndDateBorder) return;

            Logs = _allLogs.Where(x =>
                    ((DateTime)x.DateTime).Date >= startDate && ((DateTime)x.DateTime).Date <= endDate).ToList();
        }
    }
}
