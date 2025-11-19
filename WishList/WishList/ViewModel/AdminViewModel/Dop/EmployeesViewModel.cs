using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;

namespace WishList.ViewModel.AdminViewModel.Dop
{
    public class EmployeeWithOrder : INotifyPropertyChanged
    {
        private Employee _employee;
        public Employee Employee
        {
            get => _employee;
            set
            {
                _employee = value;
                OnPropertyChanged(nameof(Employee));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(LastName));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(RoleId));
                OnPropertyChanged(nameof(Role));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public int OrderNumber { get; set; }

        // Прокси-свойства для привязки - все только для чтения
        public int Id => Employee?.Id ?? 0;
        public string FirstName => Employee?.FirstName ?? string.Empty;
        public string LastName => Employee?.LastName ?? string.Empty;
        public string Email => Employee?.Email ?? string.Empty;
        public int RoleId => Employee?.RoleId ?? 0;
        public EmployeeRole Role => Employee?.Role;
        public bool IsActive => Employee?.IsActive ?? false;
        public DateTime CreatedDate => Employee?.CreatedDate ?? DateTime.MinValue;

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshProperties()
        {
            OnPropertyChanged(nameof(IsActive));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusFilterItem
    {
        public string Name { get; set; }
        public bool? IsActiveFilter { get; set; } // null = все, true = активные, false = неактивные

        public override string ToString() => Name;
    }

    public class EmployeesViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly EmployeesRepository _employeesRepository;
        private readonly EmployeeRolesRepository _rolesRepository;

        // Константы для диапазона дат
        private readonly DateTime _minDate = DateTime.Today.AddYears(-1);
        private readonly DateTime _maxDate = DateTime.Today.AddYears(1);
        private const double TrackWidth = 400;

        public EmployeesViewModel()
        {
            _context = new ApplicationContext();
            _employeesRepository = new EmployeesRepository(_context);
            _rolesRepository = new EmployeeRolesRepository(_context);

            Employees = new ObservableCollection<EmployeeWithOrder>();
            FilteredEmployees = new ObservableCollection<EmployeeWithOrder>();
            Roles = new ObservableCollection<EmployeeRole>();
            StatusFilters = new ObservableCollection<StatusFilterItem>();

            // Инициализация фильтров статуса
            InitializeStatusFilters();

            // Инициализация свойств для слайдера
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            UpdateSliderProperties();

            EmployeesView = CollectionViewSource.GetDefaultView(FilteredEmployees);
            EmployeesView.Filter = FilterEmployees;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties

        private ObservableCollection<EmployeeWithOrder> _employees;
        public ObservableCollection<EmployeeWithOrder> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                OnPropertyChanged(nameof(Employees));
            }
        }

        private ObservableCollection<EmployeeWithOrder> _filteredEmployees;
        public ObservableCollection<EmployeeWithOrder> FilteredEmployees
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

        private EmployeeWithOrder _selectedEmployee;
        public EmployeeWithOrder SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged(nameof(SelectedEmployee));
                // Обновляем доступность команд при изменении выбранного сотрудника
                CommandManager.InvalidateRequerySuggested();
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

        // ComboBox для фильтрации статуса
        private StatusFilterItem _selectedStatusFilter;
        public StatusFilterItem SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                EmployeesView?.Refresh();
                OnPropertyChanged(nameof(SelectedStatusFilter));
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

        #region Slider Properties

        private Thickness _startThumbMargin;
        public Thickness StartThumbMargin
        {
            get => _startThumbMargin;
            set
            {
                _startThumbMargin = value;
                OnPropertyChanged(nameof(StartThumbMargin));
            }
        }

        private Thickness _endThumbMargin;
        public Thickness EndThumbMargin
        {
            get => _endThumbMargin;
            set
            {
                _endThumbMargin = value;
                OnPropertyChanged(nameof(EndThumbMargin));
            }
        }

        private Thickness _selectedRangeMargin;
        public Thickness SelectedRangeMargin
        {
            get => _selectedRangeMargin;
            set
            {
                _selectedRangeMargin = value;
                OnPropertyChanged(nameof(SelectedRangeMargin));
            }
        }

        #endregion

        #region Date Properties

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                if (_filterStartDate != value)
                {
                    _filterStartDate = value;
                    OnPropertyChanged(nameof(FilterStartDate));
                    UpdateSliderProperties();
                    EmployeesView?.Refresh();
                }
            }
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                if (_filterEndDate != value)
                {
                    _filterEndDate = value;
                    OnPropertyChanged(nameof(FilterEndDate));
                    UpdateSliderProperties();
                    EmployeesView?.Refresh();
                }
            }
        }

        #endregion

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
        public ObservableCollection<StatusFilterItem> StatusFilters { get; }

        // Свойства для диалога добавления/редактирования
        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                OnPropertyChanged(nameof(IsDialogOpen));
            }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(DialogTitle));
            }
        }

        public string DialogTitle => IsEditMode ? "Редактирование сотрудника" : "Добавление нового сотрудника";

        private Employee _editingEmployee;
        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set
            {
                _editingEmployee = value;
                OnPropertyChanged(nameof(EditingEmployee));
            }
        }

        // Свойства для пароля
        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged(nameof(ConfirmPassword));
            }
        }

        // Коллекции для выпадающих списков в диалоге
        public ObservableCollection<EmployeeRole> AllRoles { get; } = new ObservableCollection<EmployeeRole>();

        #endregion

        #region Commands

        public RelayCommand LoadEmployeesCommand { get; private set; }
        public RelayCommand AddEmployeeCommand { get; private set; }
        public RelayCommand EditEmployeeCommand { get; private set; }
        public RelayCommand DeleteEmployeeCommand { get; private set; }
        public RelayCommand ActivateEmployeeCommand { get; private set; }
        public RelayCommand DeactivateEmployeeCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand SetTodayFilterCommand { get; private set; }
        public RelayCommand SetWeekFilterCommand { get; private set; }
        public RelayCommand SetMonthFilterCommand { get; private set; }
        public RelayCommand StartThumbDragDeltaCommand { get; private set; }
        public RelayCommand EndThumbDragDeltaCommand { get; private set; }
        public RelayCommand SaveEmployeeCommand { get; private set; }
        public RelayCommand CancelEditCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadEmployeesCommand = new RelayCommand(ExecuteLoadEmployees, CanExecuteLoadEmployees);
            AddEmployeeCommand = new RelayCommand(ExecuteAddEmployee);
            EditEmployeeCommand = new RelayCommand(ExecuteEditEmployee, CanExecuteEditDelete);
            DeleteEmployeeCommand = new RelayCommand(ExecuteDeleteEmployee, CanExecuteEditDelete);
            ActivateEmployeeCommand = new RelayCommand(ExecuteActivateEmployee, CanExecuteActivateDeactivate);
            DeactivateEmployeeCommand = new RelayCommand(ExecuteDeactivateEmployee, CanExecuteActivateDeactivate);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            SetTodayFilterCommand = new RelayCommand(ExecuteSetTodayFilter);
            SetWeekFilterCommand = new RelayCommand(ExecuteSetWeekFilter);
            SetMonthFilterCommand = new RelayCommand(ExecuteSetMonthFilter);
            StartThumbDragDeltaCommand = new RelayCommand(ExecuteStartThumbDragDelta);
            EndThumbDragDeltaCommand = new RelayCommand(ExecuteEndThumbDragDelta);
            SaveEmployeeCommand = new RelayCommand(ExecuteSaveEmployee);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        }

        private bool CanExecuteLoadEmployees(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedEmployee != null;
        private bool CanExecuteActivateDeactivate(object parameter) => SelectedEmployee != null;

        #endregion

        #region Helper Methods

        private void InitializeStatusFilters()
        {
            StatusFilters.Clear();
            StatusFilters.Add(new StatusFilterItem { Name = "Все сотрудники", IsActiveFilter = null });
            StatusFilters.Add(new StatusFilterItem { Name = "Только активные", IsActiveFilter = true });
            StatusFilters.Add(new StatusFilterItem { Name = "Только неактивные", IsActiveFilter = false });

            // Устанавливаем значение по умолчанию
            SelectedStatusFilter = StatusFilters[0]; // "Все сотрудники"
        }

        #endregion

        #region Slider Methods

        private void ExecuteStartThumbDragDelta(object parameter)
        {
            if (parameter is DragDeltaEventArgs e)
            {
                var newPosition = StartThumbMargin.Left + e.HorizontalChange;
                var maxPosition = EndThumbMargin.Left - 12;
                newPosition = Math.Max(0, Math.Min(newPosition, maxPosition));
                UpdateDatesFromThumbPositions(newPosition, EndThumbMargin.Left);
            }
        }

        private void ExecuteEndThumbDragDelta(object parameter)
        {
            if (parameter is DragDeltaEventArgs e)
            {
                var newPosition = EndThumbMargin.Left + e.HorizontalChange;
                var minPosition = StartThumbMargin.Left + 12;
                newPosition = Math.Max(minPosition, Math.Min(newPosition, TrackWidth - 12));
                UpdateDatesFromThumbPositions(StartThumbMargin.Left, newPosition);
            }
        }

        private void UpdateSliderProperties()
        {
            var startDate = FilterStartDate ?? _minDate;
            var endDate = FilterEndDate ?? _maxDate;

            startDate = startDate < _minDate ? _minDate : startDate;
            endDate = endDate > _maxDate ? _maxDate : endDate;

            var totalDays = (_maxDate - _minDate).TotalDays;
            var startDays = (startDate - _minDate).TotalDays;
            var endDays = (endDate - _minDate).TotalDays;

            var startRatio = startDays / totalDays;
            var endRatio = endDays / totalDays;

            var startPosition = startRatio * TrackWidth;
            var endPosition = endRatio * TrackWidth;

            if (startPosition > endPosition - 12)
            {
                startPosition = endPosition - 12;
            }

            StartThumbMargin = new Thickness(startPosition, -3, 0, 0);
            EndThumbMargin = new Thickness(endPosition, -3, 0, 0);
            SelectedRangeMargin = new Thickness(startPosition, 0, TrackWidth - endPosition - 12, 0);
        }

        public void UpdateDatesFromThumbPositions(double startPosition, double endPosition)
        {
            var startRatio = startPosition / (TrackWidth - 12);
            var endRatio = endPosition / (TrackWidth - 12);

            var totalDays = (_maxDate - _minDate).TotalDays;
            var startDate = _minDate.AddDays(totalDays * startRatio);
            var endDate = _minDate.AddDays(totalDays * endRatio);

            FilterStartDate = startDate.Date;
            FilterEndDate = endDate.Date;
        }

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

                // Добавляем сотрудников с порядковыми номерами
                int orderNumber = 1;
                foreach (var employee in employees)
                {
                    Employees.Add(new EmployeeWithOrder
                    {
                        Employee = employee,
                        OrderNumber = orderNumber++
                    });
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
                IsEditMode = false;
                EditingEmployee = new Employee
                {
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    RoleId = AllRoles.FirstOrDefault()?.Id ?? 1
                };
                // Сбрасываем пароли при открытии диалога
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                IsDialogOpen = true;
                StatusMessage = "Добавление нового сотрудника";
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
                IsEditMode = true;
                // Создаем копию сотрудника для редактирования
                EditingEmployee = new Employee
                {
                    Id = SelectedEmployee.Employee.Id,
                    FirstName = SelectedEmployee.Employee.FirstName,
                    LastName = SelectedEmployee.Employee.LastName,
                    Email = SelectedEmployee.Employee.Email,
                    RoleId = SelectedEmployee.Employee.RoleId,
                    IsActive = SelectedEmployee.Employee.IsActive,
                    CreatedDate = SelectedEmployee.Employee.CreatedDate
                };
                // Сбрасываем пароли при редактировании
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                IsDialogOpen = true;
                StatusMessage = $"Редактирование сотрудника: {SelectedEmployee.Employee.FirstName} {SelectedEmployee.Employee.LastName}";
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
                $"Вы уверены, что хотите УДАЛИТЬ сотрудника \"{SelectedEmployee.Employee.FirstName} {SelectedEmployee.Employee.LastName}\"?\n\n" +
                "Сотрудник будет ПОЛНОСТЬЮ УДАЛЕН из базы данных. Это действие нельзя отменить!",
                "Подтверждение УДАЛЕНИЯ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Проверяем, есть ли связанные задачи у сотрудника
                    using (var context = new ApplicationContext())
                    {
                        var tasksRepository = new TasksRepository(context);
                        var employeeId = SelectedEmployee.Employee.Id;

                        // Ищем задачи где сотрудник является менеджером или программистом
                        var relatedTasks = tasksRepository.Find(t => t.ManagerId == employeeId || t.ProgrammerId == employeeId)
                            .Include(t => t.Status)
                            .Include(t => t.Priority)
                            .Include(t => t.Category)
                            .Include(t => t.Client)
                            .ToList();

                        if (relatedTasks.Any())
                        {
                            var taskInfo = new StringBuilder();
                            taskInfo.AppendLine($"Сотрудник \"{SelectedEmployee.Employee.FirstName} {SelectedEmployee.Employee.LastName}\" связан со следующими задачами:");
                            taskInfo.AppendLine();

                            foreach (var task in relatedTasks)
                            {
                                var role = task.ManagerId == employeeId ? "Менеджер" : "Программист";

                                taskInfo.AppendLine($"═══════════════════════════════════════");
                                taskInfo.AppendLine($"📋 Задача #{task.Id}: {task.Title}");
                                taskInfo.AppendLine($"👤 Роль в задаче: {role}");
                                taskInfo.AppendLine($"📝 Описание: {(task.Description.Length > 100 ? task.Description.Substring(0, 100) + "..." : task.Description)}");
                                taskInfo.AppendLine($"🎯 Статус: {task.Status?.Name ?? "Не указан"}");
                                taskInfo.AppendLine($"🚨 Приоритет: {task.Priority?.Name ?? "Не указан"}");
                                taskInfo.AppendLine($"📁 Категория: {task.Category?.Name ?? "Не указан"}");
                                taskInfo.AppendLine($"📅 Дата создания: {task.CreatedDate:dd.MM.yyyy}");

                                if (task.DueDate.HasValue)
                                {
                                    var daysLeft = (task.DueDate.Value - DateTime.Now).Days;
                                    var deadlineInfo = daysLeft < 0 ? $"❌ Просрочено на {-daysLeft} дн." :
                                                      daysLeft == 0 ? "⚠️ Сегодня" :
                                                      $"⏳ Осталось {daysLeft} дн.";
                                    taskInfo.AppendLine($"⏰ Срок выполнения: {task.DueDate.Value:dd.MM.yyyy} ({deadlineInfo})");
                                }

                                if (task.CompletedDate.HasValue)
                                    taskInfo.AppendLine($"✅ Дата завершения: {task.CompletedDate.Value:dd.MM.yyyy}");

                                if (task.EstimatedHours.HasValue)
                                    taskInfo.AppendLine($"⏱️ Плановые часы: {task.EstimatedHours} ч.");

                                if (task.ActualHours.HasValue)
                                    taskInfo.AppendLine($"⏱️ Фактические часы: {task.ActualHours} ч.");

                                taskInfo.AppendLine();
                            }

                            taskInfo.AppendLine("💡 РЕШЕНИЕ: Сначала перепривяжите эти задачи другому сотруднику или завершите их.");

                            MessageBox.Show(taskInfo.ToString(),
                                "Ошибка удаления - связанные задачи",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }
                    }

                    // УДАЛЯЕМ сотрудника из базы данных
                    _employeesRepository.Delete(SelectedEmployee.Employee.Id);
                    _employeesRepository.Save();

                    // Удаляем сотрудника из коллекции
                    var employeeToRemove = Employees.FirstOrDefault(e => e.Employee.Id == SelectedEmployee.Employee.Id);
                    if (employeeToRemove != null)
                    {
                        Employees.Remove(employeeToRemove);
                    }

                    UpdateFilteredEmployees();
                    SelectedEmployee = null;

                    StatusMessage = "Сотрудник успешно УДАЛЕН из базы данных";
                }
                catch (DbUpdateException dbEx)
                {
                    // Обработка ошибок целостности данных (внешние ключи)
                    HandleDeleteError(dbEx);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления сотрудника: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления сотрудника: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Дополнительный метод для обработки ошибок удаления
        private void HandleDeleteError(DbUpdateException dbEx)
        {
            try
            {
                using (var context = new ApplicationContext())
                {
                    var tasksRepository = new TasksRepository(context);
                    var employeeId = SelectedEmployee.Employee.Id;

                    // Ищем задачи где сотрудник является менеджером или программистом
                    var relatedTasks = tasksRepository.Find(t => t.ManagerId == employeeId || t.ProgrammerId == employeeId)
                        .Include(t => t.Status)
                        .Include(t => t.Priority)
                        .Include(t => t.Category)
                        .Include(t => t.Client)
                        .ToList();

                    if (relatedTasks.Any())
                    {
                        var taskInfo = new StringBuilder();
                        taskInfo.AppendLine($"Не удалось удалить сотрудника \"{SelectedEmployee.Employee.FirstName} {SelectedEmployee.Employee.LastName}\"!");
                        taskInfo.AppendLine();
                        taskInfo.AppendLine("Сотрудник связан со следующими задачами:");
                        taskInfo.AppendLine();

                        int managerTasks = relatedTasks.Count(t => t.ManagerId == employeeId);
                        int programmerTasks = relatedTasks.Count(t => t.ProgrammerId == employeeId);

                        taskInfo.AppendLine($"📊 Статистика связей:");
                        taskInfo.AppendLine($"   • Как менеджер: {managerTasks} задач");
                        taskInfo.AppendLine($"   • Как программист: {programmerTasks} задач");
                        taskInfo.AppendLine();

                        foreach (var task in relatedTasks.Take(10)) // Показываем первые 10 задач чтобы не перегружать
                        {
                            var role = task.ManagerId == employeeId ? "👔 Менеджер" : "💻 Программист";

                            taskInfo.AppendLine($"═══════════════════════════════════════");
                            taskInfo.AppendLine($"📋 Задача #{task.Id}: {task.Title}");
                            taskInfo.AppendLine($"👤 Роль: {role}");
                            taskInfo.AppendLine($"📝 Описание: {task.Description ?? "Нет описания"}");
                            taskInfo.AppendLine($"🎯 Статус: {task.Status?.Name ?? "Не указан"}");
                            taskInfo.AppendLine($"🚨 Приоритет: {task.Priority?.Name ?? "Не указан"}");
                            taskInfo.AppendLine($"📁 Категория: {task.Category?.Name ?? "Не указан"}");
                            taskInfo.AppendLine($"📅 Дата создания: {task.CreatedDate:dd.MM.yyyy HH:mm}");

                            if (task.DueDate.HasValue)
                            {
                                var daysLeft = (task.DueDate.Value - DateTime.Now).Days;
                                var deadlineInfo = daysLeft < 0 ? $"❌ Просрочено на {-daysLeft} дн." :
                                                  daysLeft == 0 ? "⚠️ Сегодня" :
                                                  $"⏳ Осталось {daysLeft} дн.";
                                taskInfo.AppendLine($"⏰ Срок выполнения: {task.DueDate.Value:dd.MM.yyyy} ({deadlineInfo})");
                            }

                            if (task.CompletedDate.HasValue)
                                taskInfo.AppendLine($"✅ Дата завершения: {task.CompletedDate.Value:dd.MM.yyyy}");

                            if (task.EstimatedHours.HasValue)
                                taskInfo.AppendLine($"⏱️ Плановые часы: {task.EstimatedHours} ч.");

                            if (task.ActualHours.HasValue)
                                taskInfo.AppendLine($"⏱️ Фактические часы: {task.ActualHours} ч.");

                            if (task.Client != null)
                                taskInfo.AppendLine($"👥 Клиент: {task.Client.Name}");

                            taskInfo.AppendLine();
                        }

                        if (relatedTasks.Count > 10)
                        {
                            taskInfo.AppendLine($"... и еще {relatedTasks.Count - 10} задач");
                            taskInfo.AppendLine();
                        }

                        taskInfo.AppendLine("💡 РЕКОМЕНДАЦИИ:");
                        taskInfo.AppendLine("   1. Назначьте другого менеджера/программиста на эти задачи");
                        taskInfo.AppendLine("   2. Завершите или отмените задачи");
                        taskInfo.AppendLine("   3. Используйте функцию 'Перепривязка задач' в модуле задач");

                        MessageBox.Show(taskInfo.ToString(),
                            "Ошибка удаления - связанные задачи",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        StatusMessage = "Удаление невозможно - сотрудник связан с задачами";
                    }
                    else
                    {
                        // Другая ошибка базы данных
                        StatusMessage = $"Ошибка базы данных при удалении: {dbEx.Message}";
                        MessageBox.Show($"Ошибка базы данных при удалении: {dbEx.Message}\n\nВнутренняя ошибка: {dbEx.InnerException?.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при проверке связанных задач: {ex.Message}";
                MessageBox.Show($"Ошибка при проверке связанных задач: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteActivateEmployee(object parameter)
        {
            if (SelectedEmployee == null || SelectedEmployee.Employee.IsActive) return;

            try
            {
                SelectedEmployee.Employee.IsActive = true;
                _employeesRepository.Update(SelectedEmployee.Employee);
                _employeesRepository.Save();

                // Обновляем отображение
                SelectedEmployee.RefreshProperties();
                EmployeesView?.Refresh();

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
            if (SelectedEmployee == null || !SelectedEmployee.Employee.IsActive) return;

            try
            {
                SelectedEmployee.Employee.IsActive = false;
                _employeesRepository.Update(SelectedEmployee.Employee);
                _employeesRepository.Save();

                // Обновляем отображение
                SelectedEmployee.RefreshProperties();
                EmployeesView?.Refresh();

                StatusMessage = "Сотрудник успешно деактивирован";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка деактивации сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка деактивации сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveEmployee(object parameter)
        {
            try
            {
                if (EditingEmployee == null) return;

                // Валидация
                if (string.IsNullOrWhiteSpace(EditingEmployee.FirstName))
                {
                    MessageBox.Show("Имя обязательно для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingEmployee.LastName))
                {
                    MessageBox.Show("Фамилия обязательна для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingEmployee.Email))
                {
                    MessageBox.Show("Email обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация пароля только для новых сотрудников
                if (!IsEditMode)
                {
                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        MessageBox.Show("Пароль обязателен для заполнения", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (Password != ConfirmPassword)
                    {
                        MessageBox.Show("Пароли не совпадают", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (Password.Length < 6)
                    {
                        MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (IsEditMode)
                {
                    // Обновление существующего сотрудника
                    var existingEmployee = _employeesRepository.GetById(EditingEmployee.Id);
                    if (existingEmployee != null)
                    {
                        existingEmployee.FirstName = EditingEmployee.FirstName;
                        existingEmployee.LastName = EditingEmployee.LastName;
                        existingEmployee.Email = EditingEmployee.Email;
                        existingEmployee.RoleId = EditingEmployee.RoleId;
                        existingEmployee.IsActive = EditingEmployee.IsActive;

                        // Обновляем пароль только если он был введен (без хеширования)
                        if (!string.IsNullOrWhiteSpace(Password))
                        {
                            existingEmployee.PasswordHash = Password; // Сохраняем пароль как есть
                        }

                        _employeesRepository.Update(existingEmployee);
                        StatusMessage = $"Сотрудник \"{existingEmployee.FirstName} {existingEmployee.LastName}\" обновлен";
                    }
                }
                else
                {
                    // Создание нового сотрудника
                    EditingEmployee.CreatedDate = DateTime.Now;
                    // Сохраняем пароль без хеширования
                    EditingEmployee.PasswordHash = Password;

                    _employeesRepository.Create(EditingEmployee);
                    StatusMessage = $"Новый сотрудник \"{EditingEmployee.FirstName} {EditingEmployee.LastName}\" создан";
                }

                _employeesRepository.Save();
                IsDialogOpen = false;
                ExecuteLoadEmployees(null);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения сотрудника: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancelEdit(object parameter)
        {
            IsDialogOpen = false;
            EditingEmployee = null;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            StatusMessage = "Редактирование отменено";
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedStatusFilter = StatusFilters[0]; // "Все сотрудники"
            SelectedRole = null;
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            StatusMessage = "Фильтры очищены";
        }

        private void ExecuteSetTodayFilter(object parameter)
        {
            FilterStartDate = DateTime.Today;
            FilterEndDate = DateTime.Today;
        }

        private void ExecuteSetWeekFilter(object parameter)
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            FilterStartDate = startOfWeek;
            FilterEndDate = startOfWeek.AddDays(6);
        }

        private void ExecuteSetMonthFilter(object parameter)
        {
            var today = DateTime.Today;
            FilterStartDate = new DateTime(today.Year, today.Month, 1);
            FilterEndDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
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
            AllRoles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
                AllRoles.Add(role);
            }
        }

        private void UpdateFilteredEmployees()
        {
            FilteredEmployees.Clear();

            // Обновляем порядковые номера для отфильтрованных сотрудников
            int orderNumber = 1;
            foreach (var employeeWithOrder in Employees.Where(e => FilterEmployees(e)))
            {
                employeeWithOrder.OrderNumber = orderNumber++;
                FilteredEmployees.Add(employeeWithOrder);
            }
            EmployeesView?.Refresh();
        }

        private bool FilterEmployees(object obj)
        {
            if (obj is not EmployeeWithOrder employeeWithOrder) return false;
            var employee = employeeWithOrder.Employee;

            // Фильтр по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = employee.FirstName?.ToLower().Contains(searchLower) == true ||
                             employee.LastName?.ToLower().Contains(searchLower) == true ||
                             employee.Email?.ToLower().Contains(searchLower) == true ||
                             (employee.Role?.Name ?? "").ToLower().Contains(searchLower);

                if (!matches) return false;
            }

            // Фильтр по статусу (ComboBox)
            if (SelectedStatusFilter?.IsActiveFilter.HasValue == true)
            {
                if (employee.IsActive != SelectedStatusFilter.IsActiveFilter.Value)
                    return false;
            }

            // Фильтр по роли
            if (SelectedRole != null && employee.RoleId != SelectedRole.Id)
                return false;

            // Фильтр по диапазону дат
            if (FilterStartDate.HasValue && employee.CreatedDate.Date < FilterStartDate.Value.Date)
                return false;

            if (FilterEndDate.HasValue && employee.CreatedDate.Date > FilterEndDate.Value.Date)
                return false;

            return true;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}