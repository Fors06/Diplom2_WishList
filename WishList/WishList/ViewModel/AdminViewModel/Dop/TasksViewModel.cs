using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using TaskEntity = WishList.Model.Entity.Task;

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

        public string Name => Item switch
        {
            Employee e => e.Name ?? string.Empty,
            Client c => c.CompanyName ?? string.Empty,
            _ => Item?.GetType().GetProperty("Name")?.GetValue(Item)?.ToString() ?? string.Empty
        };
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        private readonly TaskWorkPlansRepository _taskWorkPlansRepository;

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
            _taskWorkPlansRepository = new TaskWorkPlansRepository(_context);

            Tasks = new ObservableCollection<TaskWithOrder>();
            FilteredTasks = new ObservableCollection<TaskWithOrder>();
            WorkPlansForDialog = new ObservableCollection<WorkPlan>();

            SelectableStatuses = new ObservableCollection<SelectableItem<TaskStatuss>>();
            SelectablePriorities = new ObservableCollection<SelectableItem<TaskPriority>>();
            SelectableManagers = new ObservableCollection<SelectableItem<Employee>>();
            SelectableClients = new ObservableCollection<SelectableItem<Client>>();
            SelectableCategories = new ObservableCollection<SelectableItem<TaskCategory>>();
            SelectableProgrammers = new ObservableCollection<SelectableItem<Employee>>();

            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            UpdateSliderProperties();

            TasksView = CollectionViewSource.GetDefaultView(FilteredTasks);
            TasksView.Filter = FilterTasks;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties
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
                if (value != null) LoadWorkPlanForSelectedOrder();
            }
        }

        private ObservableCollection<WorkPlan> _workPlansForDialog;
        public ObservableCollection<WorkPlan> WorkPlansForDialog 
        {
            get => _workPlansForDialog; 
            set 
            {
                _workPlansForDialog = value;
                OnPropertyChanged(nameof(WorkPlansForDialog)); 
            }
        }

        private WorkPlan _selectedWorkPlan;
        public WorkPlan SelectedWorkPlan 
        {
            get => _selectedWorkPlan; 
            set 
            { 
                _selectedWorkPlan = value; 
                OnPropertyChanged(nameof(SelectedWorkPlan)); 
            }
        }

        public ObservableCollection<SelectableItem<TaskStatuss>> SelectableStatuses { get; }
        public ObservableCollection<SelectableItem<TaskPriority>> SelectablePriorities { get; }
        public ObservableCollection<SelectableItem<Employee>> SelectableManagers { get; }
        public ObservableCollection<SelectableItem<Client>> SelectableClients { get; }
        public ObservableCollection<SelectableItem<TaskCategory>> SelectableCategories { get; }
        public ObservableCollection<SelectableItem<Employee>> SelectableProgrammers { get; }

        public ObservableCollection<TaskStatuss> AllStatuses { get; } = new();
        public ObservableCollection<TaskPriority> AllPriorities { get; } = new();
        public ObservableCollection<Employee> AllManagers { get; } = new();
        public ObservableCollection<Employee> AllProgrammers { get; } = new();
        public ObservableCollection<Client> AllClients { get; } = new();
        public ObservableCollection<TaskCategory> AllCategories { get; } = new();

        private SelectableItem<TaskStatuss> _selectedStatus;
        public SelectableItem<TaskStatuss> SelectedStatus 
        {
            get => _selectedStatus;
            set
            { 
                _selectedStatus = value; 
                OnPropertyChanged(nameof(SelectedStatus)); 
                TasksView?.Refresh(); 
            }
        }

        private SelectableItem<TaskPriority> _selectedPriority;
        public SelectableItem<TaskPriority> SelectedPriority 
        {
            get => _selectedPriority;
            set 
            { 
                _selectedPriority = value;
                OnPropertyChanged(nameof(SelectedPriority)); 
                TasksView?.Refresh(); 
            }
        }

        private SelectableItem<Employee> _selectedManager;
        public SelectableItem<Employee> SelectedManager 
        {
            get => _selectedManager; 
            set 
            { 
                _selectedManager = value; 
                OnPropertyChanged(nameof(SelectedManager)); 
                TasksView?.Refresh(); 
            } 
        }

        private string _searchText;
        public string SearchText 
        { 
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged(nameof(SearchText)); 
                TasksView?.Refresh(); 
            }
        }

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

        private Thickness _startThumbMargin, _endThumbMargin, _selectedRangeMargin;
        public Thickness StartThumbMargin 
        {
            get => _startThumbMargin; 
            set 
            {
                _startThumbMargin = value;
                OnPropertyChanged(nameof(StartThumbMargin)); 
            }
        }

        public Thickness EndThumbMargin 
        {
            get => _endThumbMargin;
            set 
            { 
                _endThumbMargin = value; 
                OnPropertyChanged(nameof(EndThumbMargin)); 
            }
        }

        public Thickness SelectedRangeMargin 
        { 
            get => _selectedRangeMargin; 
            set 
            {
                _selectedRangeMargin = value; 
                OnPropertyChanged(nameof(SelectedRangeMargin)); 
            }
        }

        private bool _isDialogOpen, _isEditMode, _isWorkPlanDialogOpen, _isWorkPlanFormVisible, _isEditingWorkPlan, _isLoading;
        public bool IsDialogOpen 
        {
            get => _isDialogOpen;
            set 
            {
                _isDialogOpen = value; 
                OnPropertyChanged(nameof(IsDialogOpen)); 
            }
        }

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
        private TaskEntity _editingTask;
        public TaskEntity EditingTask 
        { 
            get => _editingTask;
            set 
            { 
                _editingTask = value; 
                OnPropertyChanged(nameof(EditingTask)); 
            }
        }

        public bool IsWorkPlanDialogOpen 
        {
            get => _isWorkPlanDialogOpen; 
            set 
            {
                _isWorkPlanDialogOpen = value; 
                OnPropertyChanged(nameof(IsWorkPlanDialogOpen)); 
            }
        }

        public bool IsWorkPlanFormVisible 
        {
            get => _isWorkPlanFormVisible; 
            set 
            {
                _isWorkPlanFormVisible = value;
                OnPropertyChanged(nameof(IsWorkPlanFormVisible)); 
            }
        }

        public bool IsEditingWorkPlan 
        { 
            get => _isEditingWorkPlan; 
            set 
            {
                _isEditingWorkPlan = value; 
                OnPropertyChanged(nameof(IsEditingWorkPlan)); 
            }
        }

        private WorkPlan _currentWorkPlan;
        public WorkPlan CurrentWorkPlan 
        { 
            get => _currentWorkPlan; 
            set
            { 
                _currentWorkPlan = value; 
                OnPropertyChanged(nameof(CurrentWorkPlan)); 
            }
        }

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

        private string _selectedStatusesText = "Выберите...", _selectedPrioritiesText = "Выберите...", _selectedManagersText = "Выберите...", _selectedClientsText = "Выберите...";
        public string SelectedStatusesText 
        { 
            get => _selectedStatusesText;
            set
            {
                _selectedStatusesText = value;
                OnPropertyChanged(nameof(SelectedStatusesText)); 
            }
        }

        public string SelectedPrioritiesText 
        { 
            get => _selectedPrioritiesText; 
            set 
            {
                _selectedPrioritiesText = value;
                OnPropertyChanged(nameof(SelectedPrioritiesText)); 
            }
        }

        public string SelectedManagersText 
        { 
            get => _selectedManagersText;
            set 
            {
                _selectedManagersText = value;
                OnPropertyChanged(nameof(SelectedManagersText)); 
            }
        }

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

        #region Commands
        public ICommand LoadTasksCommand { get; private set; }
        public ICommand AddTaskCommand { get; private set; }
        public ICommand EditTaskCommand { get; private set; }
        public ICommand DeleteTaskCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ExportTasksCommand { get; private set; }
        public ICommand SetTodayFilterCommand { get; private set; }
        public ICommand SetWeekFilterCommand { get; private set; }
        public ICommand SetMonthFilterCommand { get; private set; }
        public ICommand StartThumbDragDeltaCommand { get; private set; }
        public ICommand EndThumbDragDeltaCommand { get; private set; }
        public ICommand SaveTaskCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }
        public ICommand ShowWorkPlanCommand { get; private set; }
        public ICommand CloseWorkPlanDialogCommand { get; private set; }
        public ICommand AddWorkPlanInDialogCommand { get; private set; }
        public ICommand EditWorkPlanInDialogCommand { get; private set; }
        public ICommand DeleteWorkPlanInDialogCommand { get; private set; }
        public ICommand SaveWorkPlanInDialogCommand { get; private set; }
        public ICommand CancelWorkPlanInDialogCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadTasksCommand = new RelayCommand(ExecuteLoadTasks, _ => !IsLoading);
            AddTaskCommand = new RelayCommand(ExecuteAddTask);
            EditTaskCommand = new RelayCommand(ExecuteEditTask, _ => SelectedTask != null);
            DeleteTaskCommand = new RelayCommand(ExecuteDeleteTask, _ => SelectedTask != null);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            ExportTasksCommand = new RelayCommand(ExecuteExportTasks);
            SetTodayFilterCommand = new RelayCommand(_ => { FilterStartDate = DateTime.Today; FilterEndDate = DateTime.Today; });
            SetWeekFilterCommand = new RelayCommand(_ => { var today = DateTime.Today; var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday); FilterStartDate = startOfWeek; FilterEndDate = startOfWeek.AddDays(6); });
            SetMonthFilterCommand = new RelayCommand(_ => { var today = DateTime.Today; FilterStartDate = new DateTime(today.Year, today.Month, 1); FilterEndDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)); });
            StartThumbDragDeltaCommand = new RelayCommand(ExecuteStartThumbDragDelta);
            EndThumbDragDeltaCommand = new RelayCommand(ExecuteEndThumbDragDelta);
            SaveTaskCommand = new RelayCommand(ExecuteSaveTask);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
            ShowWorkPlanCommand = new RelayCommand(ExecuteShowWorkPlan);
            CloseWorkPlanDialogCommand = new RelayCommand(_ => { IsWorkPlanDialogOpen = false; IsWorkPlanFormVisible = false; CurrentWorkPlan = null; });
            AddWorkPlanInDialogCommand = new RelayCommand(_ => { IsWorkPlanFormVisible = true; IsEditingWorkPlan = false; CurrentWorkPlan = new WorkPlan { CreatedDate = DateTime.Now, EstimatedHours = 0 }; });
            EditWorkPlanInDialogCommand = new RelayCommand(EditWorkPlanInDialog);
            DeleteWorkPlanInDialogCommand = new RelayCommand(DeleteWorkPlanInDialog);
            SaveWorkPlanInDialogCommand = new RelayCommand(SaveWorkPlanInDialog);
            CancelWorkPlanInDialogCommand = new RelayCommand(_ => { IsWorkPlanFormVisible = false; CurrentWorkPlan = null; });
        }
        #endregion

        #region Slider Methods
        private void ExecuteStartThumbDragDelta(object parameter)
        {
            if (parameter is DragDeltaEventArgs e)
            {
                var newPosition = StartThumbMargin.Left + e.HorizontalChange;
                var maxPosition = EndThumbMargin.Left - 12;
                UpdateDatesFromThumbPositions(Math.Max(0, Math.Min(newPosition, maxPosition)), EndThumbMargin.Left);
            }
        }

        private void ExecuteEndThumbDragDelta(object parameter)
        {
            if (parameter is DragDeltaEventArgs e)
            {
                var newPosition = EndThumbMargin.Left + e.HorizontalChange;
                var minPosition = StartThumbMargin.Left + 12;
                UpdateDatesFromThumbPositions(StartThumbMargin.Left, Math.Max(minPosition, Math.Min(newPosition, TrackWidth - 12)));
            }
        }

        private void UpdateSliderProperties()
        {
            var startDate = FilterStartDate ?? _minDate;
            var endDate = FilterEndDate ?? _maxDate;
            startDate = startDate < _minDate ? _minDate : startDate;
            endDate = endDate > _maxDate ? _maxDate : endDate;
            var totalDays = (_maxDate - _minDate).TotalDays;
            var startRatio = (startDate - _minDate).TotalDays / totalDays;
            var endRatio = (endDate - _minDate).TotalDays / totalDays;
            var startPosition = startRatio * TrackWidth;
            var endPosition = endRatio * TrackWidth;
            if (startPosition > endPosition - 12) startPosition = endPosition - 12;
            StartThumbMargin = new Thickness(startPosition, -3, 0, 0);
            EndThumbMargin = new Thickness(endPosition, -3, 0, 0);
            SelectedRangeMargin = new Thickness(startPosition, 0, TrackWidth - endPosition - 12, 0);
        }

        public void UpdateDatesFromThumbPositions(double startPosition, double endPosition)
        {
            var startRatio = startPosition / (TrackWidth - 12);
            var endRatio = endPosition / (TrackWidth - 12);
            var totalDays = (_maxDate - _minDate).TotalDays;
            FilterStartDate = _minDate.AddDays(totalDays * startRatio).Date;
            FilterEndDate = _minDate.AddDays(totalDays * endRatio).Date;
        }
        #endregion

        #region Command Methods
        private void ExecuteLoadTasks(object parameter)
        {
            try
            {
                IsLoading = true;
                var tasks = _tasksRepository.GetAll()
                    .Include(t => t.Client)
                    .Include(t => t.Status)
                    .Include(t => t.Priority)
                    .Include(t => t.Manager)
                    .Include(t => t.Programmer)
                    .Include(t => t.Category)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                Tasks.Clear();
                int orderNumber = 1;
                foreach (var task in tasks)
                {
                    Tasks.Add(new TaskWithOrder { Task = task, OrderNumber = orderNumber++ });
                }
                UpdateFilteredTasks();
                LoadSupportingData();
                StatusMessage = $"Загружено {Tasks.Count} задач";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { IsLoading = false; }
        }

        private void ExecuteAddTask(object parameter)
        {
            IsEditMode = false;
            EditingTask = new TaskEntity { CreatedDate = DateTime.Now, DueDate = DateTime.Now.AddDays(7), StatusId = 1, PriorityId = 2 };
            IsDialogOpen = true;
        }

        private void ExecuteEditTask(object parameter)
        {
            if (SelectedTask == null) return;
            IsEditMode = true;
            EditingTask = new TaskEntity
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
                ActualHours = SelectedTask.Task.ActualHours,
                TaskProgressId = SelectedTask.Task.TaskProgressId
            };
            IsDialogOpen = true;
        }

        private void ExecuteDeleteTask(object parameter)
        {
            if (SelectedTask == null) return;
            if (MessageBox.Show($"Удалить задачу \"{SelectedTask.Title}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                _tasksRepository.Delete(SelectedTask.Id);
                _tasksRepository.Save();
                ExecuteLoadTasks(null);
                StatusMessage = "Задача удалена";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteSaveTask(object parameter)
        {
            if (EditingTask == null) return;
            if (string.IsNullOrWhiteSpace(EditingTask.Title)) { MessageBox.Show("Введите заголовок", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                if (IsEditMode)
                {
                    var existing = _tasksRepository.GetById(EditingTask.Id);
                    if (existing != null)
                    {
                        existing.Title = EditingTask.Title;
                        existing.Description = EditingTask.Description;
                        existing.ClientId = EditingTask.ClientId;
                        existing.CategoryId = EditingTask.CategoryId;
                        existing.ManagerId = EditingTask.ManagerId;
                        existing.ProgrammerId = EditingTask.ProgrammerId;
                        existing.StatusId = EditingTask.StatusId;
                        existing.PriorityId = EditingTask.PriorityId;
                        existing.DueDate = EditingTask.DueDate;
                        existing.CompletedDate = EditingTask.CompletedDate;
                        existing.EstimatedHours = EditingTask.EstimatedHours;
                        existing.ActualHours = EditingTask.ActualHours;
                        _tasksRepository.Update(existing);
                        StatusMessage = $"Задача обновлена";
                    }
                }
                else
                {
                    var progress = new TaskProgress { ProgressPercentage = 0 };
                    _progressRepository.Create(progress);
                    _progressRepository.Save();
                    EditingTask.TaskProgressId = progress.Id;
                    EditingTask.CreatedDate = DateTime.Now;
                    _tasksRepository.Create(EditingTask);
                    StatusMessage = $"Задача создана";
                }
                _tasksRepository.Save();
                IsDialogOpen = false;
                ExecuteLoadTasks(null);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteCancelEdit(object parameter) { IsDialogOpen = false; EditingTask = null; }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedStatus = null;
            SelectedPriority = null;
            SelectedManager = null;
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            foreach (var item in SelectableStatuses) item.IsSelected = false;
            foreach (var item in SelectablePriorities) item.IsSelected = false;
            foreach (var item in SelectableManagers) item.IsSelected = false;
            foreach (var item in SelectableClients) item.IsSelected = false;
            UpdateDisplayText();
            StatusMessage = "Фильтры очищены";
        }

        private void ExecuteExportTasks(object parameter) { MessageBox.Show($"Экспорт {FilteredTasks.Count} задач", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information); }

        private void ExecuteShowWorkPlan(object parameter)
        {
            if (SelectedTask == null) { MessageBox.Show("Выберите задачу", "Информация", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            LoadWorkPlanForSelectedOrder();
            IsWorkPlanDialogOpen = true;
        }

        private void EditWorkPlanInDialog(object parameter)
        {
            if (parameter is int id)
            {
                var plan = _workPlansRepository.GetById(id);
                if (plan != null) { IsWorkPlanFormVisible = true; IsEditingWorkPlan = true; CurrentWorkPlan = plan; }
            }
        }

        private void DeleteWorkPlanInDialog(object parameter)
        {
            if (parameter is int id)
            {
                var plan = _workPlansRepository.GetById(id);
                if (plan == null) return;
                if (MessageBox.Show($"Удалить план \"{plan.PlanDescription}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                try
                {
                    var links = _taskWorkPlansRepository.Find(twp => twp.WorkPlanId == id).ToList();
                    foreach (var link in links) _taskWorkPlansRepository.Delete(link.Id);
                    _workPlansRepository.Delete(id);
                    _workPlansRepository.Save();
                    LoadWorkPlanForSelectedOrder();
                    ExecuteLoadTasks(null);
                    MessageBox.Show("План удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void SaveWorkPlanInDialog(object parameter)
        {
            if (SelectedTask == null) { MessageBox.Show("Задача не выбрана", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription)) { MessageBox.Show("Введите описание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentWorkPlan.EstimatedHours <= 0) { MessageBox.Show("Часы > 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                if (IsEditingWorkPlan)
                {
                    var existing = _workPlansRepository.GetById(CurrentWorkPlan.Id);
                    if (existing != null)
                    {
                        existing.PlanDescription = CurrentWorkPlan.PlanDescription;
                        existing.TestSteps = CurrentWorkPlan.TestSteps;
                        existing.EstimatedHours = CurrentWorkPlan.EstimatedHours;
                        _workPlansRepository.Update(existing);
                        _workPlansRepository.Save();
                    }
                }
                else
                {
                    var newPlan = new WorkPlan
                    {
                        PlanDescription = CurrentWorkPlan.PlanDescription,
                        TestSteps = CurrentWorkPlan.TestSteps,
                        EstimatedHours = CurrentWorkPlan.EstimatedHours,
                        CreatedDate = DateTime.Now
                    };
                    _workPlansRepository.Create(newPlan);
                    _workPlansRepository.Save();

                    var link = new TaskWorkPlan { TaskId = SelectedTask.Task.Id, WorkPlanId = newPlan.Id, CreatedDate = DateTime.Now };
                    _taskWorkPlansRepository.Create(link);
                    _taskWorkPlansRepository.Save();
                }
                IsWorkPlanFormVisible = false;
                LoadWorkPlanForSelectedOrder();
                ExecuteLoadTasks(null);
                MessageBox.Show(IsEditingWorkPlan ? "План обновлен" : "План создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadWorkPlanForSelectedOrder()
        {
            if (SelectedTask == null) return;
            try
            {
                WorkPlansForDialog.Clear();
                var links = _taskWorkPlansRepository.Find(twp => twp.TaskId == SelectedTask.Id).ToList();
                foreach (var link in links)
                {
                    var plan = _workPlansRepository.GetById(link.WorkPlanId);
                    if (plan != null) WorkPlansForDialog.Add(plan);
                }
                StatusMessage = WorkPlansForDialog.Any() ? $"Загружено {WorkPlansForDialog.Count} планов" : "Нет планов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        #endregion

        #region Helper Methods
        private void LoadInitialData() => ExecuteLoadTasks(null);

        private void LoadSupportingData()
        {
            void LoadItems<T>(IQueryable<T> source, ObservableCollection<SelectableItem<T>> selectableColl, ObservableCollection<T> allColl) where T : class
            {
                var items = source.ToList();
                selectableColl.Clear(); allColl.Clear();
                foreach (var item in items)
                {
                    var selectable = new SelectableItem<T> { Item = item };
                    selectable.PropertyChanged += (s, e) => { TasksView?.Refresh(); UpdateDisplayText(); };
                    selectableColl.Add(selectable); allColl.Add(item);
                }
            }
            LoadItems(_statusesRepository.GetAll(), SelectableStatuses, AllStatuses);
            LoadItems(_prioritiesRepository.GetAll(), SelectablePriorities, AllPriorities);
            LoadItems(_employeesRepository.GetAll().Where(e => e.IsActive && e.Role.Name == "Manager"), SelectableManagers, AllManagers);
            LoadItems(_employeesRepository.GetAll().Where(e => e.IsActive && e.Role.Name == "Programmer"), SelectableProgrammers, AllProgrammers);
            LoadItems(_clientsRepository.GetAll(), SelectableClients, AllClients);
            LoadItems(_categoriesRepository.GetAll(), SelectableCategories, AllCategories);
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            SelectedStatusesText = SelectableStatuses.Any(x => x.IsSelected) ? string.Join(", ", SelectableStatuses.Where(x => x.IsSelected).Select(x => x.Item.Name)) : "Выберите...";
            SelectedPrioritiesText = SelectablePriorities.Any(x => x.IsSelected) ? string.Join(", ", SelectablePriorities.Where(x => x.IsSelected).Select(x => x.Item.Name)) : "Выберите...";
            SelectedManagersText = SelectableManagers.Any(x => x.IsSelected) ? string.Join(", ", SelectableManagers.Where(x => x.IsSelected).Select(x => x.Item.Name)) : "Выберите...";
            SelectedClientsText = SelectableClients.Any(x => x.IsSelected) ? string.Join(", ", SelectableClients.Where(x => x.IsSelected).Select(x => x.Item.CompanyName)) : "Выберите...";
        }

        private void UpdateFilteredTasks()
        {
            FilteredTasks.Clear();
            foreach (var task in Tasks.Where(FilterTasks))
            {
                FilteredTasks.Add(task);
            }
            TasksView?.Refresh();
        }

        private bool FilterTasks(object obj)
        {
            if (obj is not TaskWithOrder taskWithOrder) return false;
            var task = taskWithOrder.Task;
            if (task == null) return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                if (!(task.Title?.ToLower().Contains(s) == true || task.Description?.ToLower().Contains(s) == true ||
                      (task.Client?.CompanyName ?? "").ToLower().Contains(s) || (task.Status?.Name ?? "").ToLower().Contains(s) ||
                      (task.Category?.Name ?? "").ToLower().Contains(s) || (task.Priority?.Name ?? "").ToLower().Contains(s)))
                    return false;
            }
            if (SelectedStatus?.Item != null && task.StatusId != SelectedStatus.Item.Id) return false;
            if (SelectedPriority?.Item != null && task.PriorityId != SelectedPriority.Item.Id) return false;
            if (SelectedManager?.Item != null && task.ManagerId != SelectedManager.Item.Id) return false;
            if (FilterStartDate.HasValue && task.DueDate.HasValue && task.DueDate.Value.Date < FilterStartDate.Value.Date) return false;
            if (FilterEndDate.HasValue && task.DueDate.HasValue && task.DueDate.Value.Date > FilterEndDate.Value.Date) return false;
            return true;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}