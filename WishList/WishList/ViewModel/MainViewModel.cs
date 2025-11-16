using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

namespace WishList.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentForm;
        private string _currentFormTitle;
        private string _footerText;
        private string _switchFormText;

        public object CurrentForm
        {
            get => _currentForm;
            set
            {
                _currentForm = value;
                OnPropertyChanged(nameof(CurrentForm));
            }
        }

        public string CurrentFormTitle
        {
            get => _currentFormTitle;
            set
            {
                _currentFormTitle = value;
                OnPropertyChanged(nameof(CurrentFormTitle));
            }
        }

        public string FooterText
        {
            get => _footerText;
            set
            {
                _footerText = value;
                OnPropertyChanged(nameof(FooterText));
            }
        }

        public string SwitchFormText
        {
            get => _switchFormText;
            set
            {
                _switchFormText = value;
                OnPropertyChanged(nameof(SwitchFormText));
            }
        }

        public ICommand SwitchFormCommand { get; }
        public ICommand EnterKeyCommand { get; }

        private LoginViewModel _loginViewModel;
        private RegisterViewModel _registerViewModel;
        private bool _isLoginForm = true;

        public MainViewModel()
        {
            _loginViewModel = new LoginViewModel();
            _registerViewModel = new RegisterViewModel();

            // Подписываемся на события смены формы
            _loginViewModel.SwitchToRegisterRequested += SwitchToRegister;
            _registerViewModel.SwitchToLoginRequested += SwitchToLogin;

            SwitchFormCommand = new RelayCommand(SwitchForm);
            EnterKeyCommand = new RelayCommand(ExecuteEnterKey);

            // Устанавливаем начальную форму
            ShowLoginForm();
        }

        private void ShowLoginForm()
        {
            _registerViewModel.ClearOnSwitch(); // Очищаем форму регистрации
            CurrentForm = new Views.LoginForm { DataContext = _loginViewModel };
            CurrentFormTitle = "Авторизация";
            FooterText = "Нет аккаунта?";
            SwitchFormText = "Зарегистрироваться";
            _isLoginForm = true;
        }

        private void ShowRegisterForm()
        {
            _loginViewModel.ClearOnSwitch(); // Очищаем форму логина
            CurrentForm = new Views.RegisterForm { DataContext = _registerViewModel };
            CurrentFormTitle = "Регистрация";
            FooterText = "Уже есть аккаунт?";
            SwitchFormText = "Войти";
            _isLoginForm = false;
        }

        private void SwitchForm(object obj)
        {
            if (_isLoginForm)
            {
                ShowRegisterForm();
            }
            else
            {
                ShowLoginForm();
            }
        }

        private void SwitchToRegister()
        {
            ShowRegisterForm();
        }

        private void SwitchToLogin()
        {
            ShowLoginForm();
        }

        private void ExecuteEnterKey(object obj)
        {
            if (_isLoginForm)
            {
                _loginViewModel.LoginExecute(null);
            }
            else
            {
                _registerViewModel.RegisterExecute(null);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}