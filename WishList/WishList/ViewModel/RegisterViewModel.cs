using WishList.Model.Entity;
using WishList.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace WishList.ViewModel
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;
        private int _selectedRoleId = 3;

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(nameof(SuccessMessage)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public int SelectedRoleId
        {
            get => _selectedRoleId;
            set { _selectedRoleId = value; OnPropertyChanged(nameof(SelectedRoleId)); }
        }

        public ObservableCollection<EmployeeRole> AvailableRoles { get; } = new ObservableCollection<EmployeeRole>();

        public ICommand RegisterCommand => new RelayCommand(RegisterExecute);
        public event Action SwitchToLoginRequested;

        public RegisterViewModel()
        {
            LoadAvailableRoles();
        }

        private async void LoadAvailableRoles()
        {
            try
            {
                using (var dbContext = new ApplicationContext())
                {
                    var roles = await dbContext.EmployeeRoles
                        .Where(r => r.Id != 1)
                        .ToListAsync();

                    AvailableRoles.Clear();
                    foreach (var role in roles)
                    {
                        AvailableRoles.Add(role);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                AvailableRoles.Clear();
                AvailableRoles.Add(new EmployeeRole { Id = 2, Name = "Менеджер" });
                AvailableRoles.Add(new EmployeeRole { Id = 3, Name = "Программист" });
            }
        }

        public async void RegisterExecute(object obj)
        {
            if (!ValidateInput())
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                using (var dbContext = new ApplicationContext())
                {
                    var existingUser = await dbContext.Employees
                        .FirstOrDefaultAsync(u => u.Email == Email);

                    if (existingUser != null)
                    {
                        ErrorMessage = "Пользователь с таким email уже существует.";
                        return;
                    }

                    var newEmployee = new Employee
                    {
                        FirstName = FirstName,
                        LastName = LastName,
                        Email = Email,
                        PasswordHash = Password,
                        RoleId = SelectedRoleId,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    dbContext.Employees.Add(newEmployee);
                    await dbContext.SaveChangesAsync();

                    SuccessMessage = "Аккаунт успешно создан! Вы можете войти в систему.";
                    ClearForm();

                    await System.Threading.Tasks.Task.Delay(2000);
                    SwitchToLoginRequested?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка регистрации: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = "Пожалуйста, введите имя и фамилию.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                ErrorMessage = "Пожалуйста, введите корректный email.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ErrorMessage = "Пароль должен содержать минимум 6 символов.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return false;
            }

            return true;
        }

        // Метод для очистки формы (вызывается при успешной регистрации)
        private void ClearForm()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            SelectedRoleId = 3;
        }

        // Метод для очистки при переключении форм
        public void ClearOnSwitch()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            SelectedRoleId = 3;
            IsLoading = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}