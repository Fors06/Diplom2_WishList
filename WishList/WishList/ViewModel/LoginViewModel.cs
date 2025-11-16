using WishList.Model.Entity;
using WishList.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace WishList.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        private string _realPassword = "";
        private string _errorMessage;
        private bool _isLoading;
        private bool _isPasswordVisible = false;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password => _realPassword;

        public string DisplayPassword
        {
            get => _isPasswordVisible ? _realPassword : new string('●', _realPassword.Length);
            set
            {
                if (_isPasswordVisible)
                {
                    _realPassword = value;
                }
                else
                {
                    ProcessMaskedInput(value);
                }
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordIcon));
                OnPropertyChanged(nameof(DisplayPassword));
            }
        }

        public string PasswordIcon => _isPasswordVisible ? "👁️" : "👁️‍🗨️";

        public ICommand LoginCommand => new RelayCommand(LoginExecute);
        public ICommand EnterKeyCommand => new RelayCommand(LoginExecute);
        public ICommand SwitchToRegisterCommand => new RelayCommand(SwitchToRegister);
        public ICommand TogglePasswordVisibilityCommand => new RelayCommand(ExecuteTogglePassword);

        public event Action SwitchToRegisterRequested;

        private void ProcessMaskedInput(string newValue)
        {
            int oldLength = _realPassword.Length;
            int newLength = newValue.Length;

            if (newLength > oldLength)
            {
                for (int i = oldLength; i < newLength; i++)
                {
                    if (newValue[i] != '●')
                    {
                        _realPassword += newValue[i];
                    }
                }
            }
            else if (newLength < oldLength)
            {
                _realPassword = _realPassword.Substring(0, newLength);
            }
        }

        private void SwitchToRegister(object obj)
        {
            ClearOnSwitch(); // Очищаем перед переходом
            SwitchToRegisterRequested?.Invoke();
        }

        public async void LoginExecute(object obj)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(_realPassword))
            {
                ErrorMessage = "Пожалуйста, введите имя пользователя и пароль.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                using (var dbContext = new ApplicationContext())
                {
                    Employee user = await dbContext.Employees
                        .FirstOrDefaultAsync(u => u.Email == Username && u.PasswordHash == Password);

                    if (user != null)
                    {
                        if (user.IsActive)
                        {
                            await DetermineUserRoleAndOpenWindow(user, obj);
                        }
                        else
                        {
                            ErrorMessage = "Учетная запись не активна.";
                        }
                    }
                    else
                    {
                        ErrorMessage = "Неверный логин или пароль";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task DetermineUserRoleAndOpenWindow(Employee user, object obj)
        {
            using (var dbContext = new ApplicationContext())
            {
                var userRole = await dbContext.EmployeeRoles
                    .FirstOrDefaultAsync(ur => ur.Id == user.RoleId);

                if (userRole != null)
                {
                    string roleName = userRole.Name;

                    switch (roleName)
                    {
                        case "Admin":
                            OpenAdminWindow(obj);
                            break;
                        case "Manager":
                            OpenManagerWindow(obj);
                            break;
                        default:
                            ErrorMessage = "Недостаточно прав для доступа.";
                            break;
                    }
                }
                else
                {
                    ErrorMessage = "Роль пользователя не определена.";
                }
            }
        }

        private void OpenAdminWindow(object obj)
        {
            var currentWindow = Application.Current.MainWindow as Window ?? Window.GetWindow(obj as DependencyObject);
            var adminWindow = new Views.AdminView.AdminWindow();
            Application.Current.MainWindow = adminWindow;
            adminWindow.Show();
            currentWindow?.Close();
        }

        private void OpenManagerWindow(object obj)
        {
            var currentWindow = Application.Current.MainWindow as Window ?? Window.GetWindow(obj as DependencyObject);
            var managerWindow = new Views.ManagerView.ManagerWindow();
            Application.Current.MainWindow = managerWindow;
            managerWindow.Show();
            currentWindow?.Close();
        }

        private void ExecuteTogglePassword(object parameter)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        // Метод для очистки при переключении форм
        public void ClearOnSwitch()
        {
            Username = string.Empty;
            _realPassword = "";
            ErrorMessage = string.Empty;
            IsLoading = false;
            IsPasswordVisible = false;
            OnPropertyChanged(nameof(DisplayPassword));
            OnPropertyChanged(nameof(PasswordIcon));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}