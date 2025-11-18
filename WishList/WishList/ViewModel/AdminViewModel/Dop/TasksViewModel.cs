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

                // Для Employee используем FullName
                if (Item is Employee employee)
                    return employee.Name ?? string.Empty;

                // Для Client используем CompanyName
                if (Item is Client client)
                    return client.Name ?? string.Empty;

                // Для TaskStatus и TaskPriority используем Name
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

    public class TasksViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly TasksRepository _tasksRepository;
        private readonly ClientsRepository _clientsRepository;
        private readonly TaskStatusesRepository _statusesRepository;
        private readonly TaskPrioritiesRepository _prioritiesRepository;
        private readonly EmployeesRepository _employeesRepository;

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

            Tasks = new ObservableCollection<Task>();
            FilteredTasks = new ObservableCollection<Task>();

            // Коллекции для множественного выбора
            SelectableStatuses = new ObservableCollection<SelectableItem<TaskStatuss>>();
            SelectablePriorities = new ObservableCollection<SelectableItem<TaskPriority>>();
            SelectableManagers = new ObservableCollection<SelectableItem<Employee>>();
            SelectableClients = new ObservableCollection<SelectableItem<Client>>();

            // Инициализация свойств для слайдера
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            UpdateSliderProperties();

            TasksView = CollectionViewSource.GetDefaultView(FilteredTasks);
            TasksView.Filter = FilterTasks;

            InitializeCommands();
            LoadInitialData();
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

        private ObservableCollection<Task> _tasks;
        public ObservableCollection<Task> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged(nameof(Tasks));
            }
        }

        private ObservableCollection<Task> _filteredTasks;
        public ObservableCollection<Task> FilteredTasks
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

        private Task _selectedTask;
        public Task SelectedTask
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
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
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
                StatusMessage = "Функция добавления задачи будет реализована в диалоговом окне";
                MessageBox.Show("Функция добавления задачи находится в разработке", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                StatusMessage = $"Редактирование задачи: {SelectedTask.Title}";
                MessageBox.Show($"Редактирование задачи: {SelectedTask.Title}", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                $"Вы уверены, что хотите удалить задачу \"{SelectedTask.Title}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _tasksRepository.Delete(SelectedTask.Id);
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
            foreach (var status in statuses)
            {
                var selectable = new SelectableItem<TaskStatuss> { Item = status };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableStatuses.Add(selectable);
            }

            // Загрузка приоритетов
            var priorities = _prioritiesRepository.GetAll().ToList();
            SelectablePriorities.Clear();
            foreach (var priority in priorities)
            {
                var selectable = new SelectableItem<TaskPriority> { Item = priority };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectablePriorities.Add(selectable);
            }

            // Загрузка менеджеров
            var managers = _employeesRepository.GetAll().Where(e => e.IsActive && e.Role.Name == "Manager").ToList();
            SelectableManagers.Clear();
            foreach (var manager in managers)
            {
                var selectable = new SelectableItem<Employee> { Item = manager };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableManagers.Add(selectable);
            }

            // Загрузка клиентов
            var clients = _clientsRepository.GetAll().ToList();
            SelectableClients.Clear();
            foreach (var client in clients)
            {
                var selectable = new SelectableItem<Client> { Item = client };
                selectable.PropertyChanged += (s, e) =>
                {
                    TasksView?.Refresh();
                    UpdateDisplayText();
                };
                SelectableClients.Add(selectable);
            }

            // Инициализация текста
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            // Обновление текста для статусов
            var selectedStatuses = SelectableStatuses.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedStatusesText = selectedStatuses.Any() ? string.Join(", ", selectedStatuses) : "Выберите...";

            // Обновление текста для приоритетов
            var selectedPriorities = SelectablePriorities.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedPrioritiesText = selectedPriorities.Any() ? string.Join(", ", selectedPriorities) : "Выберите...";

            // Обновление текста для менеджеров
            var selectedManagers = SelectableManagers.Where(x => x.IsSelected).Select(x => x.Item.Name);
            SelectedManagersText = selectedManagers.Any() ? string.Join(", ", selectedManagers) : "Выберите...";

            // Обновление текста для клиентов
            var selectedClients = SelectableClients.Where(x => x.IsSelected).Select(x => x.Item.CompanyName);
            SelectedClientsText = selectedClients.Any() ? string.Join(", ", selectedClients) : "Выберите...";
        }

        private void UpdateFilteredTasks()
        {
            FilteredTasks.Clear();
            foreach (var task in Tasks)
            {
                FilteredTasks.Add(task);
            }
            TasksView?.Refresh();
        }

        private bool FilterTasks(object obj)
        {
            if (obj is not Task task) return false;

            // Фильтр по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = task.Title?.ToLower().Contains(searchLower) == true ||
                             task.Description?.ToLower().Contains(searchLower) == true ||
                             (task.Client?.CompanyName ?? "").ToLower().Contains(searchLower) ||
                             (task.Status?.Name ?? "").ToLower().Contains(searchLower);

                if (!matches) return false;
            }

            // Фильтр по статусу (множественный выбор)
            var selectedStatuses = SelectableStatuses.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedStatuses.Any() && !selectedStatuses.Any(s => s.Id == task.StatusId))
                return false;

            // Фильтр по приоритету (множественный выбор)
            var selectedPriorities = SelectablePriorities.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedPriorities.Any() && !selectedPriorities.Any(p => p.Id == task.PriorityId))
                return false;

            // Фильтр по менеджеру (множественный выбор)
            var selectedManagers = SelectableManagers.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedManagers.Any() && !selectedManagers.Any(m => m.Id == task.ManagerId))
                return false;

            // Фильтр по клиенту (множественный выбор)
            var selectedClients = SelectableClients.Where(x => x.IsSelected).Select(x => x.Item).ToList();
            if (selectedClients.Any() && !selectedClients.Any(c => c.Id == task.ClientId))
                return false;

            // Фильтр по дате начала
            if (FilterStartDate.HasValue && task.DueDate.HasValue &&
                task.DueDate.Value.Date < FilterStartDate.Value.Date)
                return false;

            // Фильтр по дате окончания
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