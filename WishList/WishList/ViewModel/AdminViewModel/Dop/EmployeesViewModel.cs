using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;

namespace WishList.ViewModel.AdminViewModel.Dop
{
    public class EmployeesViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly EmployeesRepository _employeesRepository;
        private readonly EmployeeRolesRepository _rolesRepository;

        public EmployeesViewModel()
        {
            _context = new ApplicationContext();
            _employeesRepository = new EmployeesRepository(_context);
            _rolesRepository = new EmployeeRolesRepository(_context);

            Employees = new ObservableCollection<Employee>();
            FilteredEmployees = new ObservableCollection<Employee>();
            Roles = new ObservableCollection<EmployeeRole>();

            EmployeesView = CollectionViewSource.GetDefaultView(FilteredEmployees);
            EmployeesView.Filter = FilterEmployees;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties

        private ObservableCollection<Employee> _employees;
        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                OnPropertyChanged(nameof(Employees));
            }
        }

        private ObservableCollection<Employee> _filteredEmployees;
        public ObservableCollection<Employee> FilteredEmployees
        {
            get => _filteredEmployees;
            set
            {
                _filteredEmployees = value;
                EmployeesView = CollectionViewSource.GetDefaultView(FilteredEmployees);
                EmployeesView.Filter = FilterEmployees;
                OnPropertyChanged(nameof(FilteredEmployees));
            }
        }

        public ICollectionView EmployeesView { get; private set; }

        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged(nameof(SelectedEmployee));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                EmployeesView?.Refresh();
                OnPropertyChanged(nameof(SearchText));
            }
        }

        private bool _showOnlyActive = true;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set
            {
                _showOnlyActive = value;
                EmployeesView?.Refresh();
                OnPropertyChanged(nameof(ShowOnlyActive));
            }
        }

        private EmployeeRole _selectedRole;
        public EmployeeRole SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                EmployeesView?.Refresh();
                OnPropertyChanged(nameof(SelectedRole));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ObservableCollection<EmployeeRole> Roles { get; }

        #endregion

        #region Commands

        public RelayCommand LoadEmployeesCommand { get; private set; }
        public RelayCommand AddEmployeeCommand { get; private set; }
        public RelayCommand EditEmployeeCommand { get; private set; }
        public RelayCommand DeleteEmployeeCommand { get; private set; }
        public RelayCommand ActivateEmployeeCommand { get; private set; }
        public RelayCommand DeactivateEmployeeCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadEmployeesCommand = new RelayCommand(ExecuteLoadEmployees, CanExecuteLoadEmployees);
            AddEmployeeCommand = new RelayCommand(ExecuteAddEmployee);
            EditEmployeeCommand = new RelayCommand(ExecuteEditEmployee, CanExecuteEditDelete);
            DeleteEmployeeCommand = new RelayCommand(ExecuteDeleteEmployee, CanExecuteEditDelete);
            ActivateEmployeeCommand = new RelayCommand(ExecuteActivateEmployee, CanExecuteActivateDeactivate);
            DeactivateEmployeeCommand = new RelayCommand(ExecuteDeactivateEmployee, CanExecuteActivateDeactivate);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
        }

        private bool CanExecuteLoadEmployees(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedEmployee != null;
        private bool CanExecuteActivateDeactivate(object parameter) => SelectedEmployee != null;

        #endregion

        #region Command Methods

        private void ExecuteLoadEmployees(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка сотрудников...";

                var employees = _employeesRepository.GetAll()
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToList();

                Employees.Clear();
                foreach (var employee in employees)
                {
                    Employees.Add(employee);
                }
                UpdateFilteredEmployees();

                // Загружаем роли
                LoadRoles();

                StatusMessage = $"Загружено {Employees.Count} сотрудников";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки сотрудников: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddEmployee(object parameter)
        {
            try
            {
                var newEmployee = new Employee
                {
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    RoleId = Roles.FirstOrDefault()?.Id ?? 1
                };

                StatusMessage = "Добавление нового сотрудника - функция в разработке";
                MessageBox.Show("Функция добавления сотрудника находится в разработке", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка добавления сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            try
            {
                StatusMessage = $"Редактирование сотрудника: {SelectedEmployee.FirstName} {SelectedEmployee.LastName}";
                MessageBox.Show($"Редактирование сотрудника: {SelectedEmployee.FirstName} {SelectedEmployee.LastName}",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка редактирования сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка редактирования сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить сотрудника \"{SelectedEmployee.FirstName} {SelectedEmployee.LastName}\"?\n\n" +
                "Сотрудник будет помечен как неактивный и скрыт из списков.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SelectedEmployee.IsActive = false;
                    _employeesRepository.Update(SelectedEmployee);
                    _employeesRepository.Save();
                    ExecuteLoadEmployees(null);
                    StatusMessage = "Сотрудник успешно деактивирован";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления сотрудника: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления сотрудника: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteActivateEmployee(object parameter)
        {
            if (SelectedEmployee == null || SelectedEmployee.IsActive) return;

            try
            {
                SelectedEmployee.IsActive = true;
                _employeesRepository.Update(SelectedEmployee);
                _employeesRepository.Save();
                ExecuteLoadEmployees(null);
                StatusMessage = "Сотрудник успешно активирован";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка активации сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка активации сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeactivateEmployee(object parameter)
        {
            if (SelectedEmployee == null || !SelectedEmployee.IsActive) return;

            try
            {
                SelectedEmployee.IsActive = false;
                _employeesRepository.Update(SelectedEmployee);
                _employeesRepository.Save();
                ExecuteLoadEmployees(null);
                StatusMessage = "Сотрудник успешно деактивирован";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка деактивации сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка деактивации сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            ShowOnlyActive = true;
            SelectedRole = null;
            StatusMessage = "Фильтры очищены";
        }

        #endregion

        #region Helper Methods

        private void LoadInitialData()
        {
            ExecuteLoadEmployees(null);
        }

        private void LoadRoles()
        {
            var roles = _rolesRepository.GetAll().ToList();
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }
        }

        private void UpdateFilteredEmployees()
        {
            FilteredEmployees.Clear();
            foreach (var employee in Employees)
            {
                FilteredEmployees.Add(employee);
            }
            EmployeesView?.Refresh();
        }

        private bool FilterEmployees(object obj)
        {
            if (obj is not Employee employee) return false;

            // Фильтр по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = employee.FirstName?.ToLower().Contains(searchLower) == true ||
                             employee.LastName?.ToLower().Contains(searchLower) == true ||
                             employee.Email?.ToLower().Contains(searchLower) == true ||
                             employee.Role?.Name?.ToLower().Contains(searchLower) == true;

                if (!matches) return false;
            }

            // Фильтр по активности
            if (ShowOnlyActive && !employee.IsActive)
                return false;

            // Фильтр по роли
            if (SelectedRole != null && employee.RoleId != SelectedRole.Id)
                return false;

            return true;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}