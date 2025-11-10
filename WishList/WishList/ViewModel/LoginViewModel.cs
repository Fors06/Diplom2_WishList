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
                    User user = await dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == Username && u.Password == Password);

                    if (user != null)
                    {

                        if (PasswordHasher.VerifyPassword(Password, user.Password))
                        {
                            // Определяем роль и открываем соответствующее окно
                             OpenAdminWindow(obj);
                        }
                        else
                        {
                            ErrorMessage = "Неверный пароль.";
                        }
                    }
                    else
                    {
                        ErrorMessage = "Неверные данные для входа.";
                    }
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Ошибка подключения: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
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

//using System.Windows;
//using System.Windows.Input;
//using Диплом2_ITкомпания.Model.Entity;
//using Диплом2_ITкомпания.Models.Constants;
//using Диплом2_ITкомпания.Services;
//using Диплом2_ITкомпания.View.AdminView;
//using Диплом2_ITкомпания.View.ManagerView;

//namespace Диплом2_ITкомпания.ViewModels
//{
//    public class LoginViewModel : ViewModelBase
//    {
//        private string _username;
//        private string _password;
//        private string _errorMessage;
//        private bool _isLoading;
//        private readonly AuthService _authService;

//        public LoginViewModel()
//        {
//            _authService = new AuthService();
//            LoginCommand = new RelayCommand(async (obj) => await LoginExecuteAsync(obj), CanExecuteLogin);
//        }

//        public string Username
//        {
//            get => _username;
//            set => SetProperty(ref _username, value);
//        }

//        public string Password
//        {
//            get => _password;
//            set => SetProperty(ref _password, value);
//        }

//        public string ErrorMessage
//        {
//            get => _errorMessage;
//            set => SetProperty(ref _errorMessage, value);
//        }

//        public bool IsLoading
//        {
//            get => _isLoading;
//            set => SetProperty(ref _isLoading, value);
//        }

//        public ICommand LoginCommand { get; }

//        private bool CanExecuteLogin(object parameter)
//        {
//            return !string.IsNullOrEmpty(Username) &&
//                   !string.IsNullOrEmpty(Password) &&
//                   !IsLoading;
//        }

//        private async Task LoginExecuteAsync(object obj)
//        {
//            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
//            {
//                ErrorMessage = "Пожалуйста, введите имя пользователя и пароль.";
//                return;
//            }

//            try
//            {
//                IsLoading = true;
//                ErrorMessage = string.Empty;

//                var user = await _authService.AuthenticateAsync(Username, Password);

//                if (user != null)
//                {
//                    await OpenWindowBasedOnRole(user);
//                }
//                else
//                {
//                    ErrorMessage = "Неверное имя пользователя или пароль.";
//                }
//            }
//            catch (System.Exception ex)
//            {
//                ErrorMessage = $"Ошибка авторизации: {ex.Message}";
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        private async Task OpenWindowBasedOnRole(User user)
//        {
//            await Application.Current.Dispatcher.InvokeAsync(() =>
//            {
//                Window newWindow = null;

//                switch (user.Role.RoleName)
//                {
//                    case "Admin":
//                        newWindow = new AdminWindow();
//                        newWindow.DataContext = new AdminViewModel(user);
//                        break;

//                    case "Manager":
//                        newWindow = new ManagerWindow();
//                        newWindow.DataContext = new ManagerViewModel(user);
//                        break;

//                    default:
//                        ErrorMessage = "Неизвестная роль пользователя.";
//                        return;
//                }

//                // Сохраняем текущее окно
//                var currentWindow = Application.Current.MainWindow;

//                // Устанавливаем новое окно как главное
//                Application.Current.MainWindow = newWindow;
//                newWindow.Show();

//                // Закрываем старое окно
//                currentWindow?.Close();
//            });
//        }
//    }
//}