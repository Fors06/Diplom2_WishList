using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using Task = WishList.Model.Entity.Task;

namespace WishList.ViewModel.AdminViewModel.Dop
{
    public class SelectableItem<T> : INotifyPropertyChanged
    {
        private bool _isSelected;
        public T Item { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public string Name
        {
            get
            {
                if (Item == null) return string.Empty;

                if (Item is Employee employee)
                    return employee.Name ?? string.Empty;

                if (Item is Client client)
                    return client.Name ?? string.Empty;

                var nameProp = Item.GetType().GetProperty("Name");
                return nameProp?.GetValue(Item)?.ToString() ?? Item.ToString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TaskWithOrder : INotifyPropertyChanged
    {
        private Task _task;
        public Task Task
        {
            get => _task;
            set
            {
                _task = value;
                OnPropertyChanged(nameof(Task));
                // Оповещаем об изменении всех прокси-свойств при изменении задачи
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(ClientId));
                OnPropertyChanged(nameof(Client));
                OnPropertyChanged(nameof(StatusId));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(PriorityId));
                OnPropertyChanged(nameof(Priority));
                OnPropertyChanged(nameof(CreatedDate));
                OnPropertyChanged(nameof(DueDate));
                OnPropertyChanged(nameof(ManagerId));
                OnPropertyChanged(nameof(Manager));
                OnPropertyChanged(nameof(ProgrammerId));
                OnPropertyChanged(nameof(Programmer));
                OnPropertyChanged(nameof(CategoryId));
                OnPropertyChanged(nameof(Category));
                OnPropertyChanged(nameof(CompletedDate));
                OnPropertyChanged(nameof(EstimatedHours));
                OnPropertyChanged(nameof(ActualHours));
            }
        }

        public int OrderNumber { get; set; }

        // Прокси-свойства для привязки
        public int Id => Task?.Id ?? 0;
        public string Title => Task?.Title ?? string.Empty;
        public string Description => Task?.Description ?? string.Empty;
        public int? ClientId => Task?.ClientId;
        public Client Client => Task?.Client;
        public int? StatusId => Task?.StatusId;
        public TaskStatuss Status => Task?.Status;
        public int? PriorityId => Task?.PriorityId;
        public TaskPriority Priority => Task?.Priority;
        public DateTime CreatedDate => Task?.CreatedDate ?? DateTime.MinValue;
        public DateTime? DueDate => Task?.DueDate;
        public int? ManagerId => Task?.ManagerId;
        public Employee Manager => Task?.Manager;
        public int? ProgrammerId => Task?.ProgrammerId;
        public Employee Programmer => Task?.Programmer;
        public int? CategoryId => Task?.CategoryId;
        public TaskCategory Category => Task?.Category;
        public DateTime? CompletedDate => Task?.CompletedDate;
        public decimal? EstimatedHours => Task?.EstimatedHours;
        public decimal? ActualHours => Task?.ActualHours;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TasksViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly TasksRepository _tasksRepository;
        private readonly ClientsRepository _clientsRepository;
        private readonly TaskStatusesRepository _statusesRepository;
        private readonly TaskPrioritiesRepository _prioritiesRepository;
        private readonly EmployeesRepository _employeesRepository;
        private readonly TaskCategoriesRepository _categoriesRepository;
        private readonly TaskProgressRepository _progressRepository;
        private readonly WorkPlansRepository _workPlansRepository;

        // Константы для диапазона дат
        private readonly DateTime _minDate = DateTime.Today.AddYears(-1);
        private readonly DateTime _maxDate = DateTime.Today.AddYears(1);
        private const double TrackWidth = 400;

        public TasksViewModel()
        {
            _context = new ApplicationContext();
            _tasksRepository = new TasksRepository(_context);
            _clientsRepository = new ClientsRepository(_context);
            _statusesRepository = new TaskStatusesRepository(_context);
            _prioritiesRepository = new TaskPrioritiesRepository(_context);
            _employeesRepository = new EmployeesRepository(_context);
            _categoriesRepository = new TaskCategoriesRepository(_context);
            _progressRepository = new TaskProgressRepository(_context);
            _workPlansRepository = new WorkPlansRepository(_context);

            Tasks = new ObservableCollection<TaskWithOrder>();
            FilteredTasks = new ObservableCollection<TaskWithOrder>();

            // Коллекции для множественного выбора
            SelectableStatuses = new ObservableCollection<SelectableItem<TaskStatuss>>();
            SelectablePriorities = new ObservableCollection<SelectableItem<TaskPriority>>();
            SelectableManagers = new ObservableCollection<SelectableItem<Employee>>();
            SelectableClients = new ObservableCollection<SelectableItem<Client>>();
            SelectableCategories = new ObservableCollection<SelectableItem<TaskCategory>>();
            SelectableProgrammers = new ObservableCollection<SelectableItem<Employee>>();

            // Инициализация свойств для слайдера
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            UpdateSliderProperties();

            TasksView = CollectionViewSource.GetDefaultView(FilteredTasks);
            TasksView.Filter = FilterTasks;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties for Add/Edit Dialog

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

        public string DialogTitle => IsEditMode ? "Редактирование задачи" : "Добавление новой задачи";

        private Task _editingTask;
        public Task EditingTask
        {
            get => _editingTask;
            set
            {
                _editingTask = value;
                OnPropertyChanged(nameof(EditingTask));
            }
        }

        // Коллекции для выпадающих списков в диалоге
        public ObservableCollection<TaskStatuss> AllStatuses { get; } = new ObservableCollection<TaskStatuss>();
        public ObservableCollection<TaskPriority> AllPriorities { get; } = new ObservableCollection<TaskPriority>();
        public ObservableCollection<Employee> AllManagers { get; } = new ObservableCollection<Employee>();
        public ObservableCollection<Employee> AllProgrammers { get; } = new ObservableCollection<Employee>();
        public ObservableCollection<Client> AllClients { get; } = new ObservableCollection<Client>();
        public ObservableCollection<TaskCategory> AllCategories { get; } = new ObservableCollection<TaskCategory>();

        #endregion

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
                    TasksView?.Refresh();
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
                    TasksView?.Refresh();
                }
            }
        }

        #endregion

        #region Display Properties

        private string _selectedStatusesText = "Выберите...";
        public string SelectedStatusesText
        {
            get => _selectedStatusesText;
            set
            {
                _selectedStatusesText = value;
                OnPropertyChanged(nameof(SelectedStatusesText));
            }
        }

        private string _selectedPrioritiesText = "Выберите...";
        public string SelectedPrioritiesText
        {
            get => _selectedPrioritiesText;
            set
            {
                _selectedPrioritiesText = value;
                OnPropertyChanged(nameof(SelectedPrioritiesText));
            }
        }

        private string _selectedManagersText = "Выберите...";
        public string SelectedManagersText
        {
            get => _selectedManagersText;
            set
            {
                _selectedManagersText = value;
                OnPropertyChanged(nameof(SelectedManagersText));
            }
        }

        private string _selectedClientsText = "Выберите...";
        public string SelectedClientsText
        {
            get => _selectedClientsText;
            set
            {
                _selectedClientsText = value;
                OnPropertyChanged(nameof(SelectedClientsText));
            }
        }

        #endregion

        #region Other Properties

        private ObservableCollection<TaskWithOrder> _tasks;
        public ObservableCollection<TaskWithOrder> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged(nameof(Tasks));
            }
        }

        private ObservableCollection<TaskWithOrder> _filteredTasks;
        public ObservableCollection<TaskWithOrder> FilteredTasks
        {
            get => _filteredTasks;
            set
            {
                _filteredTasks = value;
                TasksView = CollectionViewSource.GetDefaultView(FilteredTasks);
                TasksView.Filter = FilterTasks;
                OnPropertyChanged(nameof(FilteredTasks));
            }
        }

        public ICollectionView TasksView { get; private set; }

        private TaskWithOrder _selectedTask;
        public TaskWithOrder SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged(nameof(SelectedTask));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                TasksView?.Refresh();
                OnPropertyChanged(nameof(SearchText));
            }
        }

        // Коллекции для множественного выбора
        public ObservableCollection<SelectableItem<TaskStatuss>> SelectableStatuses { get; }
        public ObservableCollection<SelectableItem<TaskPriority>> SelectablePriorities { get; }
        public ObservableCollection<SelectableItem<Employee>> SelectableManagers { get; }
        public ObservableCollection<SelectableItem<Client>> SelectableClients { get; }
        public ObservableCollection<SelectableItem<TaskCategory>> SelectableCategories { get; }
        public ObservableCollection<SelectableItem<Employee>> SelectableProgrammers { get; }

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

        #endregion

        #region Commands

        public RelayCommand LoadTasksCommand { get; private set; }
        public RelayCommand AddTaskCommand { get; private set; }
        public RelayCommand EditTaskCommand { get; private set; }
        public RelayCommand DeleteTaskCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand ExportTasksCommand { get; private set; }
        public RelayCommand SetTodayFilterCommand { get; private set; }
        public RelayCommand SetWeekFilterCommand { get; private set; }
        public RelayCommand SetMonthFilterCommand { get; private set; }
        public RelayCommand StartThumbDragDeltaCommand { get; private set; }
        public RelayCommand EndThumbDragDeltaCommand { get; private set; }
        public RelayCommand SaveTaskCommand { get; private set; }
        public RelayCommand CancelEditCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadTasksCommand = new RelayCommand(ExecuteLoadTasks, CanExecuteLoadTasks);
            AddTaskCommand = new RelayCommand(ExecuteAddTask);
            EditTaskCommand = new RelayCommand(ExecuteEditTask, CanExecuteEditDelete);
            DeleteTaskCommand = new RelayCommand(ExecuteDeleteTask, CanExecuteEditDelete);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            ExportTasksCommand = new RelayCommand(ExecuteExportTasks);
            SetTodayFilterCommand = new RelayCommand(ExecuteSetTodayFilter);
            SetWeekFilterCommand = new RelayCommand(ExecuteSetWeekFilter);
            SetMonthFilterCommand = new RelayCommand(ExecuteSetMonthFilter);
            StartThumbDragDeltaCommand = new RelayCommand(ExecuteStartThumbDragDelta);
            EndThumbDragDeltaCommand = new RelayCommand(ExecuteEndThumbDragDelta);
            SaveTaskCommand = new RelayCommand(ExecuteSaveTask);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        }

        private bool CanExecuteLoadTasks(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedTask != null;

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

        private void ExecuteLoadTasks(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка задач...";

                var tasks = _tasksRepository.GetAll()
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                Tasks.Clear();

                // Добавляем задачи с порядковыми номерами
                int orderNumber = 1;
                foreach (var task in tasks)
                {
                    Tasks.Add(new TaskWithOrder
                    {
                        Task = task,
                        OrderNumber = orderNumber++
                    });
                }
                UpdateFilteredTasks();

                LoadSupportingData();

                StatusMessage = $"Загружено {Tasks.Count} задач";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки задач: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddTask(object parameter)
        {
            try
            {
                IsEditMode = false;
                EditingTask = new Task
                {
                    CreatedDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(7)
                };
                IsDialogOpen = true;
                StatusMessage = "Добавление новой задачи";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления задачи: {ex.Message}";
                MessageBox.Show($"Ошибка добавления задачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditTask(object parameter)
        {
            if (SelectedTask == null) return;

            try
            {
                IsEditMode = true;
                // Создаем копию задачи для редактирования
                EditingTask = new Task
                {
                    Id = SelectedTask.Task.Id,
                    Title = SelectedTask.Task.Title,
                    Description = SelectedTask.Task.Description,
                    ClientId = SelectedTask.Task.ClientId,
                    CategoryId = SelectedTask.Task.CategoryId,
                    ManagerId = SelectedTask.Task.ManagerId,
                    ProgrammerId = SelectedTask.Task.ProgrammerId,
                    StatusId = SelectedTask.Task.StatusId,
                    PriorityId = SelectedTask.Task.PriorityId,
                    CreatedDate = SelectedTask.Task.CreatedDate,
                    DueDate = SelectedTask.Task.DueDate,
                    CompletedDate = SelectedTask.Task.CompletedDate,
                    EstimatedHours = SelectedTask.Task.EstimatedHours,
                    ActualHours = SelectedTask.Task.ActualHours
                };
                IsDialogOpen = true;
                StatusMessage = $"Редактирование задачи: {SelectedTask.Task.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка редактирования задачи: {ex.Message}";
                MessageBox.Show($"Ошибка редактирования задачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteTask(object parameter)
        {
            if (SelectedTask == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить задачу \"{SelectedTask.Task.Title}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _tasksRepository.Delete(SelectedTask.Task.Id);
                    _tasksRepository.Save();
                    ExecuteLoadTasks(null);
                    StatusMessage = "Задача успешно удалена";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления задачи: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteSaveTask(object parameter)
        {
            try
            {
                if (EditingTask == null) return;

                // Валидация
                if (string.IsNullOrWhiteSpace(EditingTask.Title))
                {
                    MessageBox.Show("Заголовок задачи обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsEditMode)
                {
                    // Обновление существующей задачи
                    var existingTask = _tasksRepository.GetById(EditingTask.Id);
                    if (existingTask != null)
                    {
                        existingTask.Title = EditingTask.Title;
                        existingTask.Description = EditingTask.Description;
                        existingTask.ClientId = EditingTask.ClientId;
                        existingTask.CategoryId = EditingTask.CategoryId;
                        existingTask.ManagerId = EditingTask.ManagerId;
                        existingTask.ProgrammerId = EditingTask.ProgrammerId;
                        existingTask.StatusId = EditingTask.StatusId;
                        existingTask.PriorityId = EditingTask.PriorityId;
                        existingTask.DueDate = EditingTask.DueDate;
                        existingTask.CompletedDate = EditingTask.CompletedDate;
                        existingTask.EstimatedHours = EditingTask.EstimatedHours;
                        existingTask.ActualHours = EditingTask.ActualHours;

                        _tasksRepository.Update(existingTask);
                        StatusMessage = $"Задача \"{existingTask.Title}\" обновлена";
                    }
                }
                else
                {
                    // Создание новой задачи
                    // Создаем связанные сущности
                    var taskProgress = new TaskProgress { ProgressPercentage = 0 };
                    _progressRepository.Create(taskProgress);
                    _progressRepository.Save();

                    var workPlan = new WorkPlan { EstimatedHours = EditingTask.EstimatedHours ?? 0 };
                    _workPlansRepository.Create(workPlan);
                    _workPlansRepository.Save();

                    EditingTask.TaskProgressId = taskProgress.Id;
                    EditingTask.WorkPlansId = workPlan.Id;
                    EditingTask.CreatedDate = DateTime.Now;

                    _tasksRepository.Create(EditingTask);
                    StatusMessage = $"Новая задача \"{EditingTask.Title}\" создана";
                }

                _tasksRepository.Save();
                IsDialogOpen = false;
                ExecuteLoadTasks(null);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения задачи: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения задачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancelEdit(object parameter)
        {
            IsDialogOpen = false;
            EditingTask = null;
            StatusMessage = "Редактирование отменено";
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);

            // Сброс множественного выбора
            foreach (var item in SelectableStatuses) item.IsSelected = false;
            foreach (var item in SelectablePriorities) item.IsSelected = false;
            foreach (var item in SelectableManagers) item.IsSelected = false;
            foreach (var item in SelectableClients) item.IsSelected = false;

            // Обновление текста
            UpdateDisplayText();

            StatusMessage = "Фильтры очищены";
        }

        private void ExecuteExportTasks(object parameter)
        {
            try
            {
                var exportCount = FilteredTasks.Count;
                StatusMessage = $"Экспортировано {exportCount} задач";
                MessageBox.Show($"Готово к экспорту {exportCount} задач", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            ExecuteLoadTasks(null);
        }

        private void LoadSupportingData()
        {
            // Загрузка статусов
            var statuses = _statusesRepository.GetAll().ToList();
            SelectableStatuses.Clear();
            AllStatuses.Clear();
            foreach (var status in statuses)
            {
                var selectable = new SelectableItem<TaskStatuss> { Item = status };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableStatuses.Add(selectable);
                AllStatuses.Add(status);
            }

            // Загрузка приоритетов
            var priorities = _prioritiesRepository.GetAll().ToList();
            SelectablePriorities.Clear();
            AllPriorities.Clear();
            foreach (var priority in priorities)
            {
                var selectable = new SelectableItem<TaskPriority> { Item = priority };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectablePriorities.Add(selectable);
                AllPriorities.Add(priority);
            }

            // Загрузка менеджеров
            var managers = _employeesRepository.GetAll().Where(e => e.IsActive && e.Role.Name == "Manager").ToList();
            SelectableManagers.Clear();
            AllManagers.Clear();
            foreach (var manager in managers)
            {
                var selectable = new SelectableItem<Employee> { Item = manager };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableManagers.Add(selectable);
                AllManagers.Add(manager);
            }

            // Загрузка программистов
            var programmers = _employeesRepository.GetAll().Where(e => e.IsActive && e.Role.Name == "Programmer").ToList();
            SelectableProgrammers.Clear();
            AllProgrammers.Clear();
            foreach (var programmer in programmers)
            {
                var selectable = new SelectableItem<Employee> { Item = programmer };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableProgrammers.Add(selectable);
                AllProgrammers.Add(programmer);
            }

            // Загрузка клиентов
            var clients = _clientsRepository.GetAll().ToList();
            SelectableClients.Clear();
            AllClients.Clear();
            foreach (var client in clients)
            {
                var selectable = new SelectableItem<Client> { Item = client };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableClients.Add(selectable);
                AllClients.Add(client);
            }

            // Загрузка категорий
            var categories = _categoriesRepository.GetAll().ToList();
            SelectableCategories.Clear();
            AllCategories.Clear();
            foreach (var category in categories)
            {
                var selectable = new SelectableItem<TaskCategory> { Item = category };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableCategories.Add(selectable);
                AllCategories.Add(category);
            }

            // Инициализация текста
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            var selectedStatuses = SelectableStatuses.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedStatusesText = selectedStatuses.Any() ? string.Join(", ", selectedStatuses) : "Выберите...";

            var selectedPriorities = SelectablePriorities.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedPrioritiesText = selectedPriorities.Any() ? string.Join(", ", selectedPriorities) : "Выберите...";

            var selectedManagers = SelectableManagers.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedManagersText = selectedManagers.Any() ? string.Join(", ", selectedManagers) : "Выберите...";

            var selectedClients = SelectableClients.Where(x => x.IsSelected).Select(x => x.Item.CompanyName);
            SelectedClientsText = selectedClients.Any() ? string.Join(", ", selectedClients) : "Выберите...";
        }

        private void UpdateFilteredTasks()
        {
            FilteredTasks.Clear();

            // Обновляем порядковые номера для отфильтрованных задач
            int orderNumber = 1;
            foreach (var taskWithOrder in Tasks.Where(t => FilterTasks(t)))
            {
                taskWithOrder.OrderNumber = orderNumber++;
                FilteredTasks.Add(taskWithOrder);
            }
            TasksView?.Refresh();
        }

        private bool FilterTasks(object obj)
        {
            if (obj is not TaskWithOrder taskWithOrder) return false;
            var task = taskWithOrder.Task;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = task.Title?.ToLower().Contains(searchLower) == true ||
                             task.Description?.ToLower().Contains(searchLower) == true ||
                             (task.Client?.CompanyName ?? "").ToLower().Contains(searchLower) ||
                             (task.Status?.Name ?? "").ToLower().Contains(searchLower);

                if (!matches) return false;
            }

            var selectedStatuses = SelectableStatuses.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedStatuses.Any() && !selectedStatuses.Any(s => s.Id == task.StatusId))
                return false;

            var selectedPriorities = SelectablePriorities.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedPriorities.Any() && !selectedPriorities.Any(p => p.Id == task.PriorityId))
                return false;

            var selectedManagers = SelectableManagers.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedManagers.Any() && !selectedManagers.Any(m => m.Id == task.ManagerId))
                return false;

            var selectedClients = SelectableClients.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedClients.Any() && !selectedClients.Any(c => c.Id == task.ClientId))
                return false;

            if (FilterStartDate.HasValue && task.DueDate.HasValue &&
                task.DueDate.Value.Date < FilterStartDate.Value.Date)
                return false;

            if (FilterEndDate.HasValue && task.DueDate.HasValue &&
                task.DueDate.Value.Date > FilterEndDate.Value.Date)
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