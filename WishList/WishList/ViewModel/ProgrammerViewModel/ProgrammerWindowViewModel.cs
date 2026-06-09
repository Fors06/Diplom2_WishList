using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WishList.Data.SwitchTheme;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using Task = WishList.Model.Entity.Task;

namespace WishList.Views.ProgrammerView
{
    public class TaskDisplayItem : INotifyPropertyChanged
    {
        private Task _task;
        private int _statusId;
        private int _orderNumber;

        public Task Task
        {
            get => _task;
            set
            {
                _task = value;
                if (value != null)
                {
                    _statusId = value.StatusId;
                }
                OnPropertyChanged(nameof(Task));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ClientName));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(ManagerName));
                OnPropertyChanged(nameof(StatusId));
                OnPropertyChanged(nameof(StatusName));
                OnPropertyChanged(nameof(PriorityId));
                OnPropertyChanged(nameof(PriorityName));
                OnPropertyChanged(nameof(DueDateString));
                OnPropertyChanged(nameof(EstimatedHoursString));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressString));
            }
        }

        public int OrderNumber
        {
            get => _orderNumber;
            set
            {
                _orderNumber = value;
                OnPropertyChanged(nameof(OrderNumber));
            }
        }

        public int Id => Task?.Id ?? 0;
        public string Title => Task?.Title ?? string.Empty;
        public string Description => Task?.Description ?? string.Empty;
        public string ClientName => Task?.Client?.CompanyName ?? "—";
        public string ManagerName => Task?.Manager?.Name ?? "—";

        public int StatusId
        {
            get => _statusId;
            set
            {
                if (_statusId != value)
                {
                    _statusId = value;
                    OnPropertyChanged(nameof(StatusId));
                    OnPropertyChanged(nameof(StatusName));
                }
            }
        }

        public string StatusName => Task?.Status?.Name ?? "—";
        public int PriorityId => Task?.PriorityId ?? 0;
        public string PriorityName => Task?.Priority?.Name ?? "—";
        public string DueDateString => Task?.DueDate?.ToString("dd.MM.yyyy") ?? "—";
        public string EstimatedHoursString => Task?.EstimatedHours?.ToString() ?? "—";

        public int ProgressPercentage
        {
            get => Task?.TaskProgress?.ProgressPercentage ?? 0;
            set
            {
                if (Task?.TaskProgress != null)
                {
                    Task.TaskProgress.ProgressPercentage = value;
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ProgressString));
                }
            }
        }

        public string ProgressString => $"{ProgressPercentage}%";

        // Публичный метод для обновления свойств извне
        public void RefreshProgress()
        {
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(ProgressString));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Делаем метод публичным
        public virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class StatusFilterItem
    {
        public string Name { get; set; }
        public int? StatusId { get; set; }
        public override string ToString() => Name;
    }

    public class StatisticCard
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Value { get; set; }
        public string Color { get; set; }
    }

    public class ProgrammerWindowViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private int _currentProgrammerId;
        private Employee _currentProgrammer;

        public ProgrammerWindowViewModel()
        {
            _context = new ApplicationContext();

            Tasks = new ObservableCollection<TaskDisplayItem>();
            FilteredTasks = new ObservableCollection<TaskDisplayItem>();
            TaskWorkPlans = new ObservableCollection<WorkPlan>();
            AllStatuses = new ObservableCollection<TaskStatuss>();
            StatusFilterItems = new ObservableCollection<StatusFilterItem>();

            InitializeStatusFilters();
            LoadCurrentProgrammer();
            LoadSupportingData();
            LoadTasks();
            UpdateStatistics();

            InitializeCommands();
        }

        #region Properties

        private ObservableCollection<TaskDisplayItem> _tasks;
        public ObservableCollection<TaskDisplayItem> Tasks
        {
            get => _tasks;
            set { _tasks = value; OnPropertyChanged(nameof(Tasks)); }
        }

        private ObservableCollection<TaskDisplayItem> _filteredTasks;
        public ObservableCollection<TaskDisplayItem> FilteredTasks
        {
            get => _filteredTasks;
            set { _filteredTasks = value; OnPropertyChanged(nameof(FilteredTasks)); }
        }

        private TaskDisplayItem _selectedTask;
        public TaskDisplayItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged(nameof(SelectedTask));
                if (value != null && value.Task != null)
                {
                    LoadWorkPlansForTask(value.Id);
                }
                else
                {
                    TaskWorkPlans.Clear();
                }
            }
        }

        private ObservableCollection<WorkPlan> _taskWorkPlans;
        public ObservableCollection<WorkPlan> TaskWorkPlans
        {
            get => _taskWorkPlans;
            set { _taskWorkPlans = value; OnPropertyChanged(nameof(TaskWorkPlans)); }
        }

        public ObservableCollection<TaskStatuss> AllStatuses { get; } = new ObservableCollection<TaskStatuss>();
        public ObservableCollection<StatusFilterItem> StatusFilterItems { get; }

        private StatusFilterItem _selectedStatusFilter;
        public StatusFilterItem SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged(nameof(SelectedStatusFilter));
                FilterTasks();
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
                FilterTasks();
            }
        }

        private ObservableCollection<StatisticCard> _statisticsCards;
        public ObservableCollection<StatisticCard> StatisticsCards
        {
            get => _statisticsCards;
            set { _statisticsCards = value; OnPropertyChanged(nameof(StatisticsCards)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public string CurrentDate => DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        public string CurrentProgrammerName => _currentProgrammer?.Name ?? "Программист";

        #endregion

        #region Commands

        public ICommand ToggleThemeCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand UpdateStatusCommand { get; private set; }

        public ICommand AddWorkPlanCommand { get; private set; }
        public ICommand EditWorkPlanCommand { get; private set; }
        public ICommand DeleteWorkPlanCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }

        private void InitializeCommands()
        {
            ToggleThemeCommand = new RelayCommand(_ => ExecuteToggleTheme());
            RefreshCommand = new RelayCommand(_ => RefreshData());
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters());
            UpdateStatusCommand = new RelayCommand(_ => ExecuteUpdateStatus());

            AddWorkPlanCommand = new RelayCommand(_ => ExecuteAddWorkPlan());
            EditWorkPlanCommand = new RelayCommand(ExecuteEditWorkPlan);
            DeleteWorkPlanCommand = new RelayCommand(ExecuteDeleteWorkPlan);
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        }

        private void ExecuteToggleTheme()
        {
            ThemeManager.ToggleTheme();
            StatusMessage = "Тема изменена";
        }

        private void RefreshData()
        {
            LoadTasks();
            UpdateStatistics();
            StatusMessage = "Данные обновлены";
        }

        private void ExecuteClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = StatusFilterItems.FirstOrDefault();
            StatusMessage = "Фильтры очищены";
        }

        #endregion

        private void ExecuteLogout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var currentWindow = Application.Current.MainWindow as Window;
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                currentWindow?.Close();

            }
        }

        #region Update Status with Auto Progress

        private void ExecuteUpdateStatus()
        {
            if (SelectedTask?.Task == null) return;

            try
            {
                // Сохраняем ID выбранной задачи ДО обновления
                int selectedTaskId = SelectedTask.Id;

                using var context = new ApplicationContext();
                var tasksRepo = new TasksRepository(context);
                var task = tasksRepo.GetById(SelectedTask.Id);

                if (task != null)
                {
                    int newStatusId = SelectedTask.StatusId;

                    if (task.StatusId != newStatusId)
                    {
                        task.StatusId = newStatusId;

                        // АВТОМАТИЧЕСКОЕ ОБНОВЛЕНИЕ ПРОГРЕССА В ЗАВИСИМОСТИ ОТ СТАТУСА
                        var progressRepo = new TaskProgressRepository(context);
                        var progress = progressRepo.GetById(task.TaskProgressId);

                        if (progress != null)
                        {
                            switch (newStatusId)
                            {
                                case 0: // New - Новая
                                    progress.ProgressPercentage = 0;
                                    break;
                                case 1: // InProgress - В работе
                                    progress.ProgressPercentage = 30;
                                    break;
                                case 2: // Testing - На тестировании
                                    progress.ProgressPercentage = 70;
                                    break;
                                case 3: // Completed - Завершена
                                    progress.ProgressPercentage = 100;
                                    task.CompletedDate = DateTime.Now;
                                    break;
                                case 4: // OnHold - На паузе
                                        // Сохраняем текущий прогресс
                                    break;
                                case 5: // Cancelled - Отменена
                                    progress.ProgressPercentage = 0;
                                    break;
                                default:
                                    break;
                            }

                            progressRepo.Update(progress);
                        }

                        tasksRepo.Update(task);
                        tasksRepo.Save();

                        // Обновляем статус в локальной копии
                        SelectedTask.Task.StatusId = newStatusId;
                        SelectedTask.StatusId = newStatusId;
                        if (SelectedTask.Task.TaskProgress != null && progress != null)
                        {
                            SelectedTask.Task.TaskProgress.ProgressPercentage = progress.ProgressPercentage;
                        }
                        SelectedTask.RefreshProgress();

                        // Обновляем список задач без полной перезагрузки
                        UpdateTaskInList(selectedTaskId);

                        var statusName = AllStatuses.FirstOrDefault(s => s.Id == newStatusId)?.Name ?? "изменен";
                        var progressValue = progress?.ProgressPercentage ?? 0;
                        StatusMessage = $"Статус задачи изменен на {statusName} (прогресс: {progressValue}%)";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка обновления статуса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTaskInList(int taskId)
        {
            // Находим задачу в коллекции
            var taskToUpdate = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (taskToUpdate != null)
            {
                // Обновляем данные из БД
                using var context = new ApplicationContext();
                var tasksRepo = new TasksRepository(context);
                var updatedTask = tasksRepo.GetById(taskId);

                if (updatedTask != null)
                {
                    taskToUpdate.Task = updatedTask;
                    taskToUpdate.StatusId = updatedTask.StatusId;

                    // Обновляем прогресс
                    if (updatedTask.TaskProgress != null)
                    {
                        taskToUpdate.ProgressPercentage = updatedTask.TaskProgress.ProgressPercentage;
                    }

                    // Обновляем фильтрованный список
                    FilterTasks();

                    // Восстанавливаем выделение
                    SelectedTask = taskToUpdate;
                }
            }
        }

        #endregion

        #region Work Plan Methods

        private void ExecuteAddWorkPlan()
        {
            if (SelectedTask == null)
            {
                MessageBox.Show("Выберите задачу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newPlan = new WorkPlan
            {
                PlanDescription = string.Empty,
                TestSteps = string.Empty,
                EstimatedHours = 1,
                CreatedDate = DateTime.Now
            };

            var dialog = new WorkPlanDialog(newPlan, false);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var context = new ApplicationContext();
                    var workPlansRepo = new WorkPlansRepository(context);
                    var taskWorkPlansRepo = new TaskWorkPlansRepository(context);

                    workPlansRepo.Create(newPlan);
                    workPlansRepo.Save();

                    var link = new TaskWorkPlan
                    {
                        TaskId = SelectedTask.Id,
                        WorkPlanId = newPlan.Id,
                        CreatedDate = DateTime.Now
                    };
                    taskWorkPlansRepo.Create(link);
                    taskWorkPlansRepo.Save();

                    LoadWorkPlansForTask(SelectedTask.Id);
                    StatusMessage = "План работ добавлен";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                    MessageBox.Show($"Ошибка добавления плана: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditWorkPlan(object parameter)
        {
            if (parameter is WorkPlan plan)
            {
                var dialog = new WorkPlanDialog(plan, true);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        using var context = new ApplicationContext();
                        var workPlansRepo = new WorkPlansRepository(context);
                        workPlansRepo.Update(plan);
                        workPlansRepo.Save();

                        if (SelectedTask != null)
                        {
                            LoadWorkPlansForTask(SelectedTask.Id);
                        }
                        StatusMessage = "План работ обновлен";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Ошибка: {ex.Message}";
                        MessageBox.Show($"Ошибка обновления плана: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ExecuteDeleteWorkPlan(object parameter)
        {
            if (parameter is int planId)
            {
                var result = MessageBox.Show("Удалить этот план работ?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ApplicationContext();
                        var workPlansRepo = new WorkPlansRepository(context);
                        var taskWorkPlansRepo = new TaskWorkPlansRepository(context);

                        var links = taskWorkPlansRepo.Find(twp => twp.WorkPlanId == planId).ToList();
                        foreach (var link in links)
                        {
                            taskWorkPlansRepo.Delete(link.Id);
                        }

                        workPlansRepo.Delete(planId);
                        workPlansRepo.Save();

                        if (SelectedTask != null)
                        {
                            LoadWorkPlansForTask(SelectedTask.Id);
                        }
                        StatusMessage = "План работ удален";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Ошибка: {ex.Message}";
                        MessageBox.Show($"Ошибка удаления плана: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        #region Load Methods

        private void LoadCurrentProgrammer()
        {
            _currentProgrammer = _context.Employees
                .FirstOrDefault(e => e.RoleId == 3 && e.IsActive);

            if (_currentProgrammer != null)
            {
                _currentProgrammerId = _currentProgrammer.Id;
            }
            else
            {
                _currentProgrammerId = 3;
            }
        }

        private void LoadTasks()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка задач...";

                var tasks = _context.Tasks
                    .Include(t => t.Client)
                    .Include(t => t.Category)
                    .Include(t => t.Status)
                    .Include(t => t.Priority)
                    .Include(t => t.Manager)
                    .Include(t => t.Programmer)
                    .Include(t => t.TaskProgress)
                    .Where(t => t.ProgrammerId == _currentProgrammerId)
                    .OrderByDescending(t => t.PriorityId)
                    .ThenBy(t => t.DueDate)
                    .ToList();

                Tasks.Clear();
                int orderNumber = 1;
                foreach (var task in tasks)
                {
                    var item = new TaskDisplayItem
                    {
                        Task = task,
                        OrderNumber = orderNumber++,
                        StatusId = task.StatusId
                    };
                    Tasks.Add(item);
                }
                FilterTasks();

                StatusMessage = $"Загружено {Tasks.Count} задач";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadWorkPlansForTask(int taskId)
        {
            try
            {
                TaskWorkPlans.Clear();

                if (taskId == 0) return;

                using var context = new ApplicationContext();
                var workPlansRepo = new WorkPlansRepository(context);
                var taskWorkPlansRepo = new TaskWorkPlansRepository(context);

                var links = taskWorkPlansRepo.Find(twp => twp.TaskId == taskId).ToList();

                foreach (var link in links)
                {
                    var plan = workPlansRepo.GetById(link.WorkPlanId);
                    if (plan != null)
                    {
                        TaskWorkPlans.Add(plan);
                    }
                }

                StatusMessage = TaskWorkPlans.Any()
                    ? $"Загружено {TaskWorkPlans.Count} планов работ"
                    : "Планы работ не найдены";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки планов: {ex.Message}";
            }
        }

        private void LoadSupportingData()
        {
            var statusesRepo = new TaskStatusesRepository(_context);
            var statuses = statusesRepo.GetAll().ToList();

            AllStatuses.Clear();
            foreach (var status in statuses)
            {
                AllStatuses.Add(status);
            }
        }

        #endregion

        #region Filters

        private void InitializeStatusFilters()
        {
            StatusFilterItems.Clear();
            StatusFilterItems.Add(new StatusFilterItem { Name = "Все задачи", StatusId = null });
            StatusFilterItems.Add(new StatusFilterItem { Name = "Новые", StatusId = 0 });
            StatusFilterItems.Add(new StatusFilterItem { Name = "В работе", StatusId = 1 });
            StatusFilterItems.Add(new StatusFilterItem { Name = "На тестировании", StatusId = 2 });
            StatusFilterItems.Add(new StatusFilterItem { Name = "Завершенные", StatusId = 3 });
            StatusFilterItems.Add(new StatusFilterItem { Name = "На паузе", StatusId = 4 });

            _selectedStatusFilter = StatusFilterItems.FirstOrDefault();
        }

        private void FilterTasks()
        {
            FilteredTasks.Clear();

            var filtered = Tasks.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(t =>
                    (t.Title?.ToLower().Contains(searchLower) == true) ||
                    (t.Description?.ToLower().Contains(searchLower) == true) ||
                    (t.ClientName?.ToLower().Contains(searchLower) == true));
            }

            if (SelectedStatusFilter?.StatusId.HasValue == true)
            {
                filtered = filtered.Where(t => t.StatusId == SelectedStatusFilter.StatusId.Value);
            }

            foreach (var task in filtered)
            {
                FilteredTasks.Add(task);
            }
        }

        #endregion

        #region Statistics

        private void UpdateStatistics()
        {
            StatisticsCards = new ObservableCollection<StatisticCard>
            {
                new StatisticCard { Icon = "📋", Title = "Всего задач", Value = Tasks.Count.ToString(), Color = "#3498DB" },
                new StatisticCard { Icon = "⚡", Title = "В работе", Value = Tasks.Count(t => t.StatusId == 1).ToString(), Color = "#F39C12" },
                new StatisticCard { Icon = "✅", Title = "Завершено", Value = Tasks.Count(t => t.StatusId == 3).ToString(), Color = "#27AE60" },
                new StatisticCard { Icon = "⏳", Title = "Просрочено", Value = Tasks.Count(t => t.DueDateString != "—" && t.Task.DueDate.Value.Date < DateTime.Today && t.StatusId != 3).ToString(), Color = "#E74C3C" }
            };
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}