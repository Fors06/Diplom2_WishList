using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using Task = WishList.Model.Entity.Task;

namespace WishList.ViewModel.AdminViewModel.Dop
{
    public class TasksViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly TasksRepository _tasksRepository;
        private readonly ClientsRepository _clientsRepository;
        private readonly TaskStatusesRepository _statusesRepository;
        private readonly TaskPrioritiesRepository _prioritiesRepository;
        private readonly EmployeesRepository _employeesRepository;

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
            Clients = new ObservableCollection<Client>();
            Statuses = new ObservableCollection<TaskStatuss>();
            Priorities = new ObservableCollection<TaskPriority>();
            Managers = new ObservableCollection<Employee>();
            Programmers = new ObservableCollection<Employee>();

            TasksView = CollectionViewSource.GetDefaultView(FilteredTasks);
            TasksView.Filter = FilterTasks;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties

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

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set
            {
                _filterStartDate = value;
                TasksView?.Refresh();
                OnPropertyChanged(nameof(FilterStartDate));
            }
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set
            {
                _filterEndDate = value;
                TasksView?.Refresh();
                OnPropertyChanged(nameof(FilterEndDate));
            }
        }

        private TaskStatuss _selectedStatus;
        public TaskStatuss SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                TasksView?.Refresh();
                OnPropertyChanged(nameof(SelectedStatus));
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

        public ObservableCollection<Client> Clients { get; }
        public ObservableCollection<TaskStatuss> Statuses { get; }
        public ObservableCollection<TaskPriority> Priorities { get; }
        public ObservableCollection<Employee> Managers { get; }
        public ObservableCollection<Employee> Programmers { get; }

        #endregion

        #region Commands

        public RelayCommand LoadTasksCommand { get; private set; }
        public RelayCommand AddTaskCommand { get; private set; }
        public RelayCommand EditTaskCommand { get; private set; }
        public RelayCommand DeleteTaskCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand ExportTasksCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadTasksCommand = new RelayCommand(ExecuteLoadTasks, CanExecuteLoadTasks);
            AddTaskCommand = new RelayCommand(ExecuteAddTask);
            EditTaskCommand = new RelayCommand(ExecuteEditTask, CanExecuteEditDelete);
            DeleteTaskCommand = new RelayCommand(ExecuteDeleteTask, CanExecuteEditDelete);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            ExportTasksCommand = new RelayCommand(ExecuteExportTasks);
        }

        private bool CanExecuteLoadTasks(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedTask != null;

        #endregion

        #region Command Methods

        private void ExecuteLoadTasks(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка задач...";

                // Загружаем задачи с включением связанных данных
                var tasks = _tasksRepository.GetAll() // Ваш репозиторий уже включает связанные данные
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                Tasks.Clear();
                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
                UpdateFilteredTasks();

                // Загружаем дополнительные данные для фильтров
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
                var newTask = new Task
                {
                    CreatedDate = DateTime.Now,
                    StatusId = Statuses.FirstOrDefault()?.Id ?? 1,
                    PriorityId = Priorities.FirstOrDefault()?.Id ?? 1,
                    ManagerId = Managers.FirstOrDefault()?.Id ?? 1
                };

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
            FilterStartDate = null;
            FilterEndDate = null;
            SelectedStatus = null;
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

        #endregion

        #region Helper Methods

        private void LoadInitialData()
        {
            ExecuteLoadTasks(null);
        }

        private void LoadSupportingData()
        {
            // Загрузка клиентов
            var clients = _clientsRepository.GetAll().ToList();
            Clients.Clear();
            foreach (var client in clients) Clients.Add(client);

            // Загрузка статусов
            var statuses = _statusesRepository.GetAll().ToList();
            Statuses.Clear();
            foreach (var status in statuses) Statuses.Add(status);

            // Загрузка приоритетов
            var priorities = _prioritiesRepository.GetAll().ToList();
            Priorities.Clear();
            foreach (var priority in priorities) Priorities.Add(priority);

            // Загрузка сотрудников
            var employees = _employeesRepository.GetAll().Where(e => e.IsActive).ToList();
            Managers.Clear();
            Programmers.Clear();
            foreach (var employee in employees)
            {
                if (employee.RoleId == 1) // Менеджеры
                    Managers.Add(employee);
                else if (employee.RoleId == 2) // Программисты
                    Programmers.Add(employee);
            }
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
                             (task.Client?.CompanyName ?? "").ToLower().Contains(searchLower);
                             (task.Status?.Name ?? "").ToLower().Contains(searchLower);

                if (!matches) return false;
            }

            // Фильтр по статусу
            if (SelectedStatus != null && task.StatusId != SelectedStatus.Id)
                return false;

            // Фильтр по дате начала
            if (FilterStartDate.HasValue && task.CreatedDate.Date < FilterStartDate.Value.Date)
                return false;

            // Фильтр по дате окончания
            if (FilterEndDate.HasValue && task.CreatedDate.Date > FilterEndDate.Value.Date)
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