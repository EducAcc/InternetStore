using EdPr.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EdPr.ViewModels
{
    public class AdminAddEmployeeViewModel : ViewModelBase
    {
        private readonly EdPrEntities _db;
        private readonly UserService _userService;
        private readonly bool _isEditMode;
        private readonly string _oldLogin;

        public UserProfileViewModel UserProfileVM { get; }
        public event Action<bool> EmployeeSaved;

        public List<EmployeeOccupation> Occupations { get; }
        public List<Subdivision> Subdivisions { get; }

        private EmployeeOccupation _selectedOccupation;
        public EmployeeOccupation SelectedOccupation
        {
            get => _selectedOccupation;
            set { _selectedOccupation = value; OnPropertyChanged(); }
        }

        private Subdivision _selectedSubdivision;
        public Subdivision SelectedSubdivision
        {
            get => _selectedSubdivision;
            set { _selectedSubdivision = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }

        public AdminAddEmployeeViewModel(EdPrEntities db) : this(db, null)
        {
        }

        public AdminAddEmployeeViewModel(EdPrEntities db, User employee)
        {
            _db = db;
            _userService = new UserService(db);
            _isEditMode = employee != null;
            _oldLogin = employee?.Login ?? "";

            Occupations = _db.EmployeeOccupations.ToList();
            Subdivisions = _db.Subdivisions.ToList();

            if (_isEditMode)
            {
                UserProfileVM = new UserProfileViewModel(_userService, employee);
                SelectedOccupation = Occupations.FirstOrDefault(x => x.Id == employee.Occupation);
                SelectedSubdivision = Subdivisions.FirstOrDefault(x => x.Id == employee.SubdivisionId);
            }
            else
            {
                UserProfileVM = new UserProfileViewModel(_userService, new User { Account = new Account() });
            }

            SaveCommand = new RelayCommand(_ => ExecuteSave());
        }

        private void ExecuteSave()
        {
            var user = UserProfileVM.User;
            var password = UserProfileVM.Password;

            if (!_userService.ValidateCommonFields(user, password, _oldLogin, out string err))
            {
                MessageBox.Show(err);
                return;
            }

            if (SelectedOccupation == null)
            {
                MessageBox.Show("Выберите должность");
                return;
            }

            user.Occupation = SelectedOccupation.Id;
            user.SubdivisionId = SelectedSubdivision?.Id;
            user.EmployeeOccupation = SelectedOccupation;
            user.Subdivision = SelectedSubdivision;

            _userService.PrepareUserAccount(user, password, _oldLogin);

            try
            {
                if (!_isEditMode)
                {
                    _userService.EnsureUserCart(user);
                    _db.Users.Add(user);
                }

                _db.SaveChanges();

                MessageBox.Show(_isEditMode
                    ? "Данные сотрудника успешно обновлены!"
                    : "Сотрудник успешно добавлен администратором!");

                UserProfileVM.Password = string.Empty;
                EmployeeSaved?.Invoke(!_isEditMode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}");
            }
        }
    }
}
