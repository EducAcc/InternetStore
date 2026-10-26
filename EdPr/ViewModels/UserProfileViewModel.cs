using EdPr.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace EdPr.ViewModels
{
    public class UserProfileViewModel : ViewModelBase
    {
        private UserService _userService;
        private User _user;
        private string _password;
        private bool _isRequestMode = false;

        private bool _isFemale = true;
        private bool _isMale = false;


        public bool IsFemale
        {
            get => _isFemale;
            set
            {
                _isFemale = value;
                OnPropertyChanged();

                OnSelectionGenderRadioBtn();
            }
        }

        public bool IsMale
        {
            get => _isMale;
            set
            {
                _isMale = value;
                OnPropertyChanged();

                OnSelectionGenderRadioBtn();
            }
        }

        public User User 
        { 
            get => _user; 
            set 
            { 
                _user = value;

                if (value?.Address == null && value != null)
                {
                    value.Address = new Address();
                }

                if (value?.Gender != null)
                {
                    _isFemale = value.Gender == true;
                    _isMale = value.Gender == false;
                    OnPropertyChanged(nameof(IsFemale));
                    OnPropertyChanged(nameof(IsMale));
                }

                OnPropertyChanged(); 
                OnPropertyChanged(nameof(Photo)); 
            } 
        }

        public bool IsRequestMode 
        { 
            get => _isRequestMode; 
            set
            {
                _isRequestMode = value;
                OnPropertyChanged();
            }
        }

        public string Password 
        { 
            get => _password; 
            set 
            { _password = value; 
                OnPropertyChanged(); 
            } 
        }
     
        public BitmapImage Photo => _userService.LoadPhoto(User?.Photo);

        public ICommand UploadPhotoCommand { get; }

        public UserProfileViewModel(UserService userService, User user)
        {
            _userService = userService;

            if (user?.Gender != null)
            {
                _isFemale = user.Gender == true;
                _isMale = user.Gender == false;
            }

            User = user;
            
            if (!IsRequestMode)
            {
                UploadPhotoCommand = new RelayCommand(_ => {
                    var bytes = _userService.SelectPhoto();
                    if (bytes != null) { User.Photo = bytes; OnPropertyChanged(nameof(Photo)); }
                });
            }
        }

        private void OnSelectionGenderRadioBtn()
        {
            if (User != null)
            {
                User.Gender = _isFemale;
            }
        }
    }
}
