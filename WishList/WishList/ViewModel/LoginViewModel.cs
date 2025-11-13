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
        private string _password;
        private string _errorMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged(nameof(Username));
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public ICommand LoginCommand => new RelayCommand(LoginExecute);
        public ICommand EnterKeyCommand => new RelayCommand(LoginExecute);


        public LoginViewModel()
        {
        }

        public async void LoginExecute(object obj)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
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
                    // Ищем пользователя по email
                    Employee user = await dbContext.Employees
                        .FirstOrDefaultAsync(u => u.Email == Username);

                    if (user != null)
                    {
                        // Проверяем пароль с помощью PasswordHasher
                        if (PasswordHasher.VerifyPassword(Password, user.PasswordHash))
                        {
                            // Проверяем активность пользователя
                            if (user.IsActive)
                            {
                                // Определяем роль и открываем соответствующее окно
                                await DetermineUserRoleAndOpenWindow(user, obj);
                            }
                            else
                            {
                                ErrorMessage = "Учетная запись не активна.";
                            }
                        }
                        else
                        {
                            ErrorMessage = "Неверный пароль.";
                        }
                    }
                    else
                    {
                        ErrorMessage = "Пользователь не найден.";
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
                // Предполагаемая структура таблицы ролей
                var userRole = await dbContext.EmployeeRoles
                    .Include(ur => ur.Name)
                    .FirstOrDefaultAsync(ur => ur.Id == user.Id);

                if (userRole != null)
                {
                    switch (userRole.Name.ToLower())
                    {
                        case "Администратор":
                            OpenAdminWindow(obj);
                            break;
                        case "Менеджер":
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


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}