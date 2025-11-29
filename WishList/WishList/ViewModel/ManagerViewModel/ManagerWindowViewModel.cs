using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using WishList.Data.SwitchTheme;
using Task = WishList.Model.Entity.Task;

namespace WishList.ViewModel.ManagerViewModel
{
    public class ManagerWindowViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly ClientsRepository _clientsRepository;
        private readonly TasksRepository _tasksRepository;
        private readonly TaskCategoriesRepository _categoriesRepository;
        private readonly TaskPrioritiesRepository _prioritiesRepository;
        private readonly TaskStatusesRepository _statusesRepository;
        private readonly WorkPlansRepository _workPlansRepository;
        private readonly EmployeesRepository _employeesRepository;
        private readonly TaskProgressRepository _progressRepository;

        private string _clientSearchText;
        private string _orderSearchText;
        private string _workPlanSearchText;
        private bool _isLoading;
        private string _statusMessage;
        private bool _isClientFormVisible;
        private bool _isOrderFormVisible;
        private bool _isWorkPlanFormVisible;
        private bool _isEditingClient;
        private bool _isEditingOrder;
        private bool _isEditingWorkPlan;
        private Client _currentClient;
        private Task _currentOrder;
        private WorkPlan _currentWorkPlan;
        private bool _isWorkPlanDialogOpen;
        private TaskWithOrder _selectedOrder;
        private WorkPlanWithOrder _selectedWorkPlan;

        public ManagerWindowViewModel()
        {
            _context = new ApplicationContext();
            _clientsRepository = new ClientsRepository(_context);
            _tasksRepository = new TasksRepository(_context);
            _categoriesRepository = new TaskCategoriesRepository(_context);
            _prioritiesRepository = new TaskPrioritiesRepository(_context);
            _statusesRepository = new TaskStatusesRepository(_context);
            _workPlansRepository = new WorkPlansRepository(_context);
            _employeesRepository = new EmployeesRepository(_context);
            _progressRepository = new TaskProgressRepository(_context);

            Clients = new ObservableCollection<ClientWithOrder>();
            Orders = new ObservableCollection<TaskWithOrder>();
            WorkPlans = new ObservableCollection<WorkPlanWithOrder>();
            FilteredClients = new ObservableCollection<ClientWithOrder>();
            FilteredOrders = new ObservableCollection<TaskWithOrder>();
            FilteredWorkPlans = new ObservableCollection<WorkPlanWithOrder>();
            AllStatuses = new ObservableCollection<TaskStatuss>();
            AllPriorities = new ObservableCollection<TaskPriority>();
            AllCategories = new ObservableCollection<TaskCategory>();
            AllClients = new ObservableCollection<Client>();
            AllManagers = new ObservableCollection<Employee>();
            AllProgrammers = new ObservableCollection<Employee>();
            StatisticsCards = new ObservableCollection<StatisticCard>();
            WorkPlansForDialog = new ObservableCollection<WorkPlanWithOrder>();

            CurrentClient = new Client();
            CurrentOrder = new Task();
            CurrentWorkPlan = new WorkPlan();

            InitializeCommands();
            LoadData();
            UpdateStatistics();
        }

        #region Properties

        public ObservableCollection<ClientWithOrder> Clients { get; }
        public ObservableCollection<TaskWithOrder> Orders { get; }
        public ObservableCollection<WorkPlanWithOrder> WorkPlans { get; }
        public ObservableCollection<ClientWithOrder> FilteredClients { get; }
        public ObservableCollection<TaskWithOrder> FilteredOrders { get; }
        public ObservableCollection<WorkPlanWithOrder> FilteredWorkPlans { get; }
        public ObservableCollection<TaskStatuss> AllStatuses { get; }
        public ObservableCollection<TaskPriority> AllPriorities { get; }
        public ObservableCollection<TaskCategory> AllCategories { get; }
        public ObservableCollection<Client> AllClients { get; }
        public ObservableCollection<Employee> AllManagers { get; }
        public ObservableCollection<Employee> AllProgrammers { get; }
        public ObservableCollection<StatisticCard> StatisticsCards { get; }
        public ObservableCollection<WorkPlanWithOrder> WorkPlansForDialog { get; }



        public string CurrentDate => DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        public string ClientSearchText
        {
            get => _clientSearchText;
            set
            {
                _clientSearchText = value;
                OnPropertyChanged(nameof(ClientSearchText));
                FilterClients();
            }
        }

        public string OrderSearchText
        {
            get => _orderSearchText;
            set
            {
                _orderSearchText = value;
                OnPropertyChanged(nameof(OrderSearchText));
                FilterOrders();
            }
        }

        public string WorkPlanSearchText
        {
            get => _workPlanSearchText;
            set
            {
                _workPlanSearchText = value;
                OnPropertyChanged(nameof(WorkPlanSearchText));
                FilterWorkPlans();
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

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool IsClientFormVisible
        {
            get => _isClientFormVisible;
            set
            {
                _isClientFormVisible = value;
                OnPropertyChanged(nameof(IsClientFormVisible));
            }
        }

        public bool IsOrderFormVisible
        {
            get => _isOrderFormVisible;
            set
            {
                _isOrderFormVisible = value;
                OnPropertyChanged(nameof(IsOrderFormVisible));
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

        public bool IsEditingClient
        {
            get => _isEditingClient;
            set
            {
                _isEditingClient = value;
                OnPropertyChanged(nameof(IsEditingClient));
                OnPropertyChanged(nameof(ClientFormTitle));
            }
        }

        public bool IsEditingOrder
        {
            get => _isEditingOrder;
            set
            {
                _isEditingOrder = value;
                OnPropertyChanged(nameof(IsEditingOrder));
                OnPropertyChanged(nameof(OrderFormTitle));
            }
        }

        public bool IsEditingWorkPlan
        {
            get => _isEditingWorkPlan;
            set
            {
                _isEditingWorkPlan = value;
                OnPropertyChanged(nameof(IsEditingWorkPlan));
                OnPropertyChanged(nameof(WorkPlanFormTitle));
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

        public string ClientFormTitle => IsEditingClient ? "Редактирование клиента" : "Добавление нового клиента";
        public string OrderFormTitle => IsEditingOrder ? "Редактирование заказа" : "Создание нового заказа";
        public string WorkPlanFormTitle => IsEditingWorkPlan ? "Редактирование плана работ" : "Создание нового плана работ";

        public Client CurrentClient
        {
            get => _currentClient;
            set
            {
                _currentClient = value;
                OnPropertyChanged(nameof(CurrentClient));
            }
        }

        public Task CurrentOrder
        {
            get => _currentOrder;
            set
            {
                _currentOrder = value;
                OnPropertyChanged(nameof(CurrentOrder));
            }
        }

        public WorkPlan CurrentWorkPlan
        {
            get => _currentWorkPlan;
            set
            {
                _currentWorkPlan = value;
                OnPropertyChanged(nameof(CurrentWorkPlan));
            }
        }

        public TaskWithOrder SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged(nameof(SelectedOrder));
                // При изменении выбранного заказа можно обновить связанные данные
                if (value != null)
                {
                    LoadWorkPlanForSelectedOrder();
                }
            }
        }

        public WorkPlanWithOrder SelectedWorkPlan
        {
            get => _selectedWorkPlan;
            set
            {
                _selectedWorkPlan = value;
                OnPropertyChanged(nameof(SelectedWorkPlan));
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        #endregion

        #region Commands

        public ICommand ToggleThemeCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand AddClientCommand { get; private set; }
        public ICommand EditClientCommand { get; private set; }
        public ICommand DeleteClientCommand { get; private set; }
        public ICommand SaveClientCommand { get; private set; }
        public ICommand CancelClientCommand { get; private set; }
        public ICommand RefreshClientsCommand { get; private set; }
        public ICommand AddOrderCommand { get; private set; }
        public ICommand EditOrderCommand { get; private set; }
        public ICommand DeleteOrderCommand { get; private set; }
        public ICommand SaveOrderCommand { get; private set; }
        public ICommand CancelOrderCommand { get; private set; }
        public ICommand RefreshOrdersCommand { get; private set; }
        public ICommand ShowWorkPlanCommand { get; private set; }
        public ICommand AddWorkPlanCommand { get; private set; }
        public ICommand EditWorkPlanCommand { get; private set; }
        public ICommand DeleteWorkPlanCommand { get; private set; }
        public ICommand SaveWorkPlanCommand { get; private set; }
        public ICommand CancelWorkPlanCommand { get; private set; }
        public ICommand RefreshWorkPlansCommand { get; private set; }
        public ICommand CloseWorkPlanDialogCommand { get; private set; }
        public ICommand AddWorkPlanInDialogCommand { get; private set; }
        public ICommand EditWorkPlanInDialogCommand { get; private set; }
        public ICommand DeleteWorkPlanInDialogCommand { get; private set; }
        public ICommand SaveWorkPlanInDialogCommand { get; private set; }
        public ICommand CancelWorkPlanInDialogCommand { get; private set; }

        private void InitializeCommands()
        {
            ToggleThemeCommand = new RelayCommand(_ => ExecuteToggleTheme());
            RefreshCommand = new RelayCommand(_ => RefreshData());
            AddClientCommand = new RelayCommand(_ => AddClient());
            EditClientCommand = new RelayCommand(EditClient);
            DeleteClientCommand = new RelayCommand(DeleteClient);
            SaveClientCommand = new RelayCommand(_ => SaveClient());
            CancelClientCommand = new RelayCommand(_ => CancelClient());
            RefreshClientsCommand = new RelayCommand(_ => RefreshClients());
            AddOrderCommand = new RelayCommand(_ => AddOrder());
            EditOrderCommand = new RelayCommand(EditOrder);
            DeleteOrderCommand = new RelayCommand(DeleteOrder);
            SaveOrderCommand = new RelayCommand(_ => SaveOrder());
            CancelOrderCommand = new RelayCommand(_ => CancelOrder());
            RefreshOrdersCommand = new RelayCommand(_ => RefreshOrders());
            ShowWorkPlanCommand = new RelayCommand(_ => ShowWorkPlan());
            AddWorkPlanCommand = new RelayCommand(_ => AddWorkPlan());
            EditWorkPlanCommand = new RelayCommand(EditWorkPlan);
            DeleteWorkPlanCommand = new RelayCommand(DeleteWorkPlan);
            SaveWorkPlanCommand = new RelayCommand(_ => SaveWorkPlan());
            CancelWorkPlanCommand = new RelayCommand(_ => CancelWorkPlan());
            RefreshWorkPlansCommand = new RelayCommand(_ => RefreshWorkPlans());

            CloseWorkPlanDialogCommand = new RelayCommand(_ => CloseWorkPlanDialog());
            AddWorkPlanInDialogCommand = new RelayCommand(_ => AddWorkPlanInDialog());
            EditWorkPlanInDialogCommand = new RelayCommand(EditWorkPlanInDialog);
            DeleteWorkPlanInDialogCommand = new RelayCommand(DeleteWorkPlanInDialog);
            SaveWorkPlanInDialogCommand = new RelayCommand(_ => SaveWorkPlanInDialog());
            CancelWorkPlanInDialogCommand = new RelayCommand(_ => CancelWorkPlanInDialog());
        }

        #endregion

        #region Command Methods

        private void AddWorkPlan()
        {
            IsWorkPlanFormVisible = true;
            IsEditingWorkPlan = false;
            CurrentWorkPlan = new WorkPlan
            {
                CreatedDate = DateTime.Now
            };
            StatusMessage = "Создание нового плана работ";
        }

        private void EditWorkPlan(object parameter)
        {
            if (parameter is int workPlanId)
            {
                var workPlan = _workPlansRepository.GetById(workPlanId);
                if (workPlan != null)
                {
                    IsWorkPlanFormVisible = true;
                    IsEditingWorkPlan = true;
                    CurrentWorkPlan = workPlan;
                    StatusMessage = $"Редактирование плана работ: {workPlan.PlanDescription}";
                }
            }
        }

        private void DeleteWorkPlan(object parameter)
        {
            if (parameter is int workPlanId)
            {
                var workPlan = _workPlansRepository.GetById(workPlanId);
                if (workPlan != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить план работ \"{workPlan.PlanDescription}\"?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _workPlansRepository.Delete(workPlanId);
                            _workPlansRepository.Save();
                            LoadWorkPlans();
                            StatusMessage = "План работ успешно удален";
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Ошибка удаления плана работ: {ex.Message}";
                            MessageBox.Show($"Ошибка удаления плана работ: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        public void SaveWorkPlan()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription))
            {
                MessageBox.Show("Введите описание плана работ", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditingWorkPlan)
                {
                    _workPlansRepository.Update(CurrentWorkPlan);
                    StatusMessage = $"План работ \"{CurrentWorkPlan.PlanDescription}\" обновлен";
                }
                else
                {
                    _workPlansRepository.Create(CurrentWorkPlan);
                    StatusMessage = $"Новый план работ \"{CurrentWorkPlan.PlanDescription}\" создан";
                }

                _workPlansRepository.Save();
                IsWorkPlanFormVisible = false;
                LoadWorkPlans();

                MessageBox.Show(IsEditingWorkPlan ? "План работ успешно обновлен" : "План работ успешно создан",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения плана работ: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения плана работ: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelWorkPlan()
        {
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Редактирование плана работ отменено";
        }

        public void RefreshWorkPlans()
        {
            LoadWorkPlans();
        }

        private void ShowWorkPlan()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для просмотра плана работ", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LoadWorkPlanForSelectedOrder();
            IsWorkPlanDialogOpen = true;
            StatusMessage = $"Просмотр плана работ для заказа: {SelectedOrder.Title}";
        }

        private void CloseWorkPlanDialog()
        {
            IsWorkPlanDialogOpen = false;
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Диалог планов работ закрыт";
        }

        private void AddWorkPlanInDialog()
        {
            IsWorkPlanFormVisible = true;
            IsEditingWorkPlan = false;
            CurrentWorkPlan = new WorkPlan
            {
                CreatedDate = DateTime.Now
            };
            StatusMessage = "Создание плана работ для выбранного заказа";
        }

        private void EditWorkPlanInDialog(object parameter)
        {
            if (parameter is int workPlanId)
            {
                var workPlan = _workPlansRepository.GetById(workPlanId);
                if (workPlan != null)
                {
                    IsWorkPlanFormVisible = true;
                    IsEditingWorkPlan = true;
                    CurrentWorkPlan = workPlan;
                    StatusMessage = $"Редактирование плана работ: {workPlan.PlanDescription}";
                }
            }
        }

        private void DeleteWorkPlanInDialog(object parameter)
        {
            if (parameter is int workPlanId)
            {
                try
                {
                    var workPlan = _workPlansRepository.GetById(workPlanId);
                    if (workPlan == null)
                    {
                        MessageBox.Show("План работ не найден", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить план работ \"{workPlan.PlanDescription}\"?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    // Просто удаляем план работ без обработки связей
                    _workPlansRepository.Delete(workPlanId);
                    _workPlansRepository.Save();

                    // Обновляем UI
                    LoadWorkPlanForSelectedOrder();

                    StatusMessage = "План работ успешно удален";
                    MessageBox.Show("План работ успешно удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления плана работ: {ex.Message}";
                    MessageBox.Show($"Ошибка удаления плана работ: {ex.Message}\n\n" +
                                  "Если план работ связан с заказом, сначала отвяжите его.",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveWorkPlanInDialog()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription))
            {
                MessageBox.Show("Введите описание плана работ", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentWorkPlan.EstimatedHours <= 0)
            {
                MessageBox.Show("Введите количество часов (больше 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditingWorkPlan)
                {
                    _workPlansRepository.Update(CurrentWorkPlan);
                    StatusMessage = $"План работ \"{CurrentWorkPlan.PlanDescription}\" обновлен";
                }
                else
                {
                    _workPlansRepository.Create(CurrentWorkPlan);
                    _workPlansRepository.Save();

                    if (SelectedOrder != null)
                    {
                        var order = _tasksRepository.GetById(SelectedOrder.Id);
                        if (order != null)
                        {
                            order.WorkPlansId = CurrentWorkPlan.Id;
                            _tasksRepository.Update(order);
                        }
                    }

                    StatusMessage = $"Новый план работ \"{CurrentWorkPlan.PlanDescription}\" создан";
                }

                _tasksRepository.Save();
                IsWorkPlanFormVisible = false;
                LoadWorkPlanForSelectedOrder();

                MessageBox.Show(IsEditingWorkPlan ? "План работ успешно обновлен" : "План работ успешно создан",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения плана работ: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения плана работ: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelWorkPlanInDialog()
        {
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Редактирование плана работ отменено";
        }

        private void LoadWorkPlanForSelectedOrder()
        {
            if (SelectedOrder == null) return;

            try
            {
                var order = _tasksRepository.GetAll()
                    .Include(t => t.WorkPlan)
                    .FirstOrDefault(t => t.Id == SelectedOrder.Id);

                WorkPlansForDialog.Clear();

                if (order?.WorkPlan != null)
                {
                    WorkPlansForDialog.Add(new WorkPlanWithOrder
                    {
                        WorkPlan = order.WorkPlan,
                        OrderNumber = 1
                    });
                }

                StatusMessage = order?.WorkPlan != null
                    ? "Загружен план работ для заказа"
                    : "Для этого заказа нет плана работ";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки плана работ: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки плана работ: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteToggleTheme()
        {
            try
            {
                ThemeManager.ToggleTheme();
                var currentTheme = ThemeManager.GetCurrentTheme();
                StatusMessage = $"Тема изменена на {(currentTheme ? "тёмную" : "светлую")}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка смены темы: {ex.Message}";
                MessageBox.Show($"Ошибка смены темы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshData()
        {
            LoadData();
            UpdateStatistics();
            StatusMessage = "Данные обновлены";
        }

        public void LoadData()
        {
            LoadClients();
            LoadOrders();
            LoadWorkPlans();
            LoadComboBoxData();
        }

        private void LoadClients()
        {
            try
            {
                IsLoading = true;
                var clients = _clientsRepository.GetAll()
                    .OrderBy(c => c.CompanyName)
                    .ToList();

                Clients.Clear();
                int orderNumber = 1;
                foreach (var client in clients)
                {
                    Clients.Add(new ClientWithOrder
                    {
                        Client = client,
                        OrderNumber = orderNumber++
                    });
                }
                FilterClients();

                StatusMessage = $"Загружено {Clients.Count} клиентов";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки клиентов: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadOrders()
        {
            try
            {
                IsLoading = true;
                var orders = _tasksRepository.GetAll()
                    .Include(t => t.Client)
                    .Include(t => t.Category)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .Include(t => t.Manager)
                    .Include(t => t.Programmer)
                    .Include(t => t.WorkPlan)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                Orders.Clear();
                int orderNumber = 1;
                foreach (var order in orders)
                {
                    Orders.Add(new TaskWithOrder
                    {
                        Task = order,
                        OrderNumber = orderNumber++
                    });
                }
                FilterOrders();

                StatusMessage = $"Загружено {Orders.Count} заказов";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки заказов: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadWorkPlans()
        {
            try
            {
                IsLoading = true;
                var workPlans = _workPlansRepository.GetAll()
                    .OrderByDescending(wp => wp.CreatedDate)
                    .ToList();

                WorkPlans.Clear();
                int orderNumber = 1;
                foreach (var workPlan in workPlans)
                {
                    WorkPlans.Add(new WorkPlanWithOrder
                    {
                        WorkPlan = workPlan,
                        OrderNumber = orderNumber++
                    });
                }
                FilterWorkPlans();

                StatusMessage = $"Загружено {WorkPlans.Count} планов работ";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки планов работ: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки планов работ: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                // Загрузка клиентов
                var clients = _clientsRepository.GetAll()
                    .OrderBy(c => c.CompanyName)
                    .ToList();

                AllClients.Clear();
                foreach (var client in clients)
                {
                    AllClients.Add(client);
                }

                // Загрузка категорий
                var categories = _categoriesRepository.GetAll()
                    .OrderBy(c => c.Name)
                    .ToList();

                AllCategories.Clear();
                foreach (var category in categories)
                {
                    AllCategories.Add(category);
                }

                // Загрузка приоритетов
                var priorities = _prioritiesRepository.GetAll()
                    .OrderBy(p => p.Id)
                    .ToList();

                AllPriorities.Clear();
                foreach (var priority in priorities)
                {
                    AllPriorities.Add(priority);
                }

                // Загрузка статусов
                var statuses = _statusesRepository.GetAll()
                    .OrderBy(s => s.Id)
                    .ToList();

                AllStatuses.Clear();
                foreach (var status in statuses)
                {
                    AllStatuses.Add(status);
                }

                // Загрузка менеджеров
                var managers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role.Name == "Manager")
                    .OrderBy(e => e.Name)
                    .ToList();

                AllManagers.Clear();
                foreach (var manager in managers)
                {
                    AllManagers.Add(manager);
                }

                // Загрузка программистов
                var programmers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role.Name == "Programmer")
                    .OrderBy(e => e.Name)
                    .ToList();

                AllProgrammers.Clear();
                foreach (var programmer in programmers)
                {
                    AllProgrammers.Add(programmer);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        public void AddClient()
        {
            IsClientFormVisible = true;
            IsEditingClient = false;
            CurrentClient = new Client { CreatedDate = DateTime.Now };
            StatusMessage = "Добавление нового клиента";
        }

        private void EditClient(object parameter)
        {
            if (parameter is int clientId)
            {
                var client = _clientsRepository.GetById(clientId);
                if (client != null)
                {
                    IsClientFormVisible = true;
                    IsEditingClient = true;
                    CurrentClient = client;
                    StatusMessage = $"Редактирование клиента: {client.CompanyName}";
                }
            }
        }

        private void DeleteClient(object parameter)
        {
            if (parameter is int clientId)
            {
                var client = _clientsRepository.GetById(clientId);
                if (client != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить клиента \"{client.CompanyName}\"?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _clientsRepository.Delete(clientId);
                            _clientsRepository.Save();
                            LoadClients();
                            LoadComboBoxData();
                            StatusMessage = "Клиент успешно удален";
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Ошибка удаления клиента: {ex.Message}";
                            MessageBox.Show($"Ошибка удаления клиента: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        public void SaveClient()
        {
            if (string.IsNullOrWhiteSpace(CurrentClient.CompanyName))
            {
                MessageBox.Show("Введите название компании", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentClient.Email))
            {
                MessageBox.Show("Введите email клиента", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditingClient)
                {
                    _clientsRepository.Update(CurrentClient);
                    StatusMessage = $"Клиент \"{CurrentClient.CompanyName}\" обновлен";
                }
                else
                {
                    _clientsRepository.Create(CurrentClient);
                    StatusMessage = $"Новый клиент \"{CurrentClient.CompanyName}\" создан";
                }

                _clientsRepository.Save();
                IsClientFormVisible = false;
                LoadClients();
                LoadComboBoxData();

                MessageBox.Show(IsEditingClient ? "Клиент успешно обновлен" : "Клиент успешно создан",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения клиента: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelClient()
        {
            IsClientFormVisible = false;
            CurrentClient = new Client();
            StatusMessage = "Редактирование клиента отменено";
        }

        public void AddOrder()
        {
            try
            {
                IsOrderFormVisible = true;
                IsEditingOrder = false;

                CurrentOrder = new Task
                {
                    CreatedDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(7),
                    StatusId = 1, // Новый
                    PriorityId = 2, // Средний
                    ManagerId = GetCurrentManagerId(),
                    EstimatedHours = 8,
                    ActualHours = 0
                };

                StatusMessage = "Создание нового заказа";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления заказа: {ex.Message}";
                MessageBox.Show($"Ошибка добавления заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditOrder(object parameter)
        {
            if (parameter is int orderId)
            {
                try
                {
                    var order = _tasksRepository.GetAll()
                        .Include(t => t.Client)
                        .Include(t => t.Category)
                        .Include(t => t.Priority)
                        .Include(t => t.Status)
                        .Include(t => t.Manager)
                        .Include(t => t.Programmer)
                        .FirstOrDefault(t => t.Id == orderId);

                    if (order != null)
                    {
                        IsOrderFormVisible = true;
                        IsEditingOrder = true;

                        // Создаем копию для редактирования
                        CurrentOrder = new Task
                        {
                            Id = order.Id,
                            Title = order.Title,
                            Description = order.Description,
                            ClientId = order.ClientId,
                            CategoryId = order.CategoryId,
                            PriorityId = order.PriorityId,
                            StatusId = order.StatusId,
                            ManagerId = order.ManagerId,
                            ProgrammerId = order.ProgrammerId,
                            DueDate = order.DueDate,
                            CreatedDate = order.CreatedDate,
                            CompletedDate = order.CompletedDate,
                            EstimatedHours = order.EstimatedHours,
                            ActualHours = order.ActualHours,
                            TaskProgressId = order.TaskProgressId,
                            WorkPlansId = order.WorkPlansId
                        };

                        StatusMessage = $"Редактирование заказа: {order.Title}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка редактирования заказа: {ex.Message}";
                    MessageBox.Show($"Ошибка редактирования заказа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteOrder(object parameter)
        {
            if (parameter is int orderId)
            {
                var order = _tasksRepository.GetById(orderId);
                if (order != null)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите удалить заказ \"{order.Title}\"?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _tasksRepository.Delete(orderId);
                            _tasksRepository.Save();
                            LoadOrders();
                            StatusMessage = "Заказ успешно удален";
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"Ошибка удаления заказа: {ex.Message}";
                            MessageBox.Show($"Ошибка удаления заказа: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        public void SaveOrder()
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(CurrentOrder.Title))
                {
                    MessageBox.Show("Введите название заказа", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.ClientId == 0 || CurrentOrder.ClientId == null)
                {
                    MessageBox.Show("Выберите клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.CategoryId == 0 || CurrentOrder.CategoryId == null)
                {
                    MessageBox.Show("Выберите категорию", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.PriorityId == 0 || CurrentOrder.PriorityId == null)
                {
                    MessageBox.Show("Выберите приоритет", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.StatusId == 0 || CurrentOrder.StatusId == null)
                {
                    MessageBox.Show("Выберите статус", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.ManagerId == 0 || CurrentOrder.ManagerId == null)
                {
                    MessageBox.Show("Выберите менеджера", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.DueDate.HasValue && CurrentOrder.DueDate.Value < DateTime.Today)
                {
                    MessageBox.Show("Дата выполнения не может быть в прошлом", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CurrentOrder.EstimatedHours <= 0)
                {
                    MessageBox.Show("Введите корректное количество часов", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsEditingOrder)
                {
                    // Обновление существующего заказа
                    var existingOrder = _tasksRepository.GetById(CurrentOrder.Id);
                    if (existingOrder != null)
                    {
                        existingOrder.Title = CurrentOrder.Title;
                        existingOrder.Description = CurrentOrder.Description;
                        existingOrder.ClientId = CurrentOrder.ClientId;
                        existingOrder.CategoryId = CurrentOrder.CategoryId;
                        existingOrder.PriorityId = CurrentOrder.PriorityId;
                        existingOrder.StatusId = CurrentOrder.StatusId;
                        existingOrder.ManagerId = CurrentOrder.ManagerId;
                        existingOrder.ProgrammerId = CurrentOrder.ProgrammerId;
                        existingOrder.DueDate = CurrentOrder.DueDate;
                        existingOrder.CompletedDate = CurrentOrder.CompletedDate;
                        existingOrder.EstimatedHours = CurrentOrder.EstimatedHours;
                        existingOrder.ActualHours = CurrentOrder.ActualHours;

                        _tasksRepository.Update(existingOrder);
                        StatusMessage = $"Заказ \"{existingOrder.Title}\" обновлен";
                    }
                }
                else
                {
                    // Создание нового заказа
                    // Создаем связанные сущности
                    var taskProgress = new TaskProgress
                    {
                        ProgressPercentage = 0,
                        CreatedDate = DateTime.Now
                    };
                    _progressRepository.Create(taskProgress);
                    _progressRepository.Save();

                    var workPlan = new WorkPlan
                    {
                        PlanDescription = $"План работ для: {CurrentOrder.Title}",
                        EstimatedHours = (decimal)CurrentOrder.EstimatedHours,
                        CreatedDate = DateTime.Now
                    };
                    _workPlansRepository.Create(workPlan);
                    _workPlansRepository.Save();

                    // Устанавливаем ID связанных сущностей
                    CurrentOrder.TaskProgressId = taskProgress.Id;
                    CurrentOrder.WorkPlansId = workPlan.Id;
                    CurrentOrder.CreatedDate = DateTime.Now;

                    _tasksRepository.Create(CurrentOrder);
                    StatusMessage = $"Новый заказ \"{CurrentOrder.Title}\" создан";
                }

                _tasksRepository.Save();
                IsOrderFormVisible = false;
                LoadOrders();

                MessageBox.Show(IsEditingOrder ? "Заказ успешно обновлен" : "Заказ успешно создан",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения заказа: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelOrder()
        {
            IsOrderFormVisible = false;
            CurrentOrder = new Task();
            StatusMessage = "Редактирование заказа отменено";
        }

        public void RefreshClients()
        {
            LoadClients();
        }

        public void RefreshOrders()
        {
            LoadOrders();
        }

        #endregion

        #region Filter Methods

        private void FilterClients()
        {
            FilteredClients.Clear();
            var filtered = string.IsNullOrEmpty(ClientSearchText)
                ? Clients
                : Clients.Where(c =>
                    c.CompanyName.Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.ContactPerson.Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var client in filtered)
            {
                FilteredClients.Add(client);
            }
        }

        private void FilterOrders()
        {
            FilteredOrders.Clear();
            var filtered = string.IsNullOrEmpty(OrderSearchText)
                ? Orders
                : Orders.Where(o =>
                    o.Title.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Client.CompanyName.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Category.Name.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Priority.Name.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var order in filtered)
            {
                FilteredOrders.Add(order);
            }
        }

        private void FilterWorkPlans()
        {
            FilteredWorkPlans.Clear();
            var filtered = string.IsNullOrEmpty(WorkPlanSearchText)
                ? WorkPlans
                : WorkPlans.Where(wp =>
                    wp.PlanDescription.Contains(WorkPlanSearchText, StringComparison.OrdinalIgnoreCase) ||
                    wp.TestSteps.Contains(WorkPlanSearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var workPlan in filtered)
            {
                FilteredWorkPlans.Add(workPlan);
            }
        }

        #endregion

        #region Statistics

        private void UpdateStatistics()
        {
            StatisticsCards.Clear();

            var totalClients = Clients.Count;
            var totalOrders = Orders.Count;
            var activeOrders = Orders.Count(o => o.Status?.Name != "Завершено");
            var totalWorkPlans = WorkPlans.Count;

            StatisticsCards.Add(new StatisticCard
            {
                Icon = "👥",
                Title = "Клиенты",
                Value = totalClients.ToString(),
                Description = "Всего клиентов",
                Color = "#3498DB"
            });

            StatisticsCards.Add(new StatisticCard
            {
                Icon = "📦",
                Title = "Заказы",
                Value = totalOrders.ToString(),
                Description = "Всего заказов",
                Color = "#2ECC71"
            });

            StatisticsCards.Add(new StatisticCard
            {
                Icon = "⚡",
                Title = "Активные",
                Value = activeOrders.ToString(),
                Description = "Активных заказов",
                Color = "#F39C12"
            });

            StatisticsCards.Add(new StatisticCard
            {
                Icon = "📋",
                Title = "Планы",
                Value = totalWorkPlans.ToString(),
                Description = "Планов работ",
                Color = "#9B59B6"
            });
        }

        #endregion

        #region Helper Methods

        private int GetCurrentManagerId()
        {
            var currentManager = _employeesRepository.GetAll()
                .FirstOrDefault(e => e.IsActive && e.Role.Name == "Manager");
            return currentManager?.Id ?? 1;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Helper Classes

    public class ClientWithOrder
    {
        public Client Client { get; set; }
        public int OrderNumber { get; set; }

        public int Id => Client?.Id ?? 0;
        public string CompanyName => Client?.CompanyName ?? string.Empty;
        public string ContactPerson => Client?.ContactPerson ?? string.Empty;
        public string Email => Client?.Email ?? string.Empty;
        public string Phone => Client?.Phone ?? string.Empty;
        public string Address => Client?.Address ?? string.Empty;
        public DateTime CreatedDate => Client?.CreatedDate ?? DateTime.MinValue;
    }

    public class TaskWithOrder
    {
        public Task Task { get; set; }
        public int OrderNumber { get; set; }

        public int Id => Task?.Id ?? 0;
        public string Title => Task?.Title ?? string.Empty;
        public Client Client => Task?.Client;
        public TaskCategory Category => Task?.Category;
        public TaskPriority Priority => Task?.Priority;
        public TaskStatuss Status => Task?.Status;
        public DateTime? DueDate => Task?.DueDate;
        public DateTime CreatedDate => Task?.CreatedDate ?? DateTime.MinValue;
        public WorkPlan WorkPlan => Task?.WorkPlan;
    }

    public class WorkPlanWithOrder
    {
        public WorkPlan WorkPlan { get; set; }
        public int OrderNumber { get; set; }

        public int Id => WorkPlan?.Id ?? 0;
        public string PlanDescription => WorkPlan?.PlanDescription ?? string.Empty;
        public string TestSteps => WorkPlan?.TestSteps ?? string.Empty;
        public decimal EstimatedHours => WorkPlan?.EstimatedHours ?? 0;
        public DateTime CreatedDate => WorkPlan?.CreatedDate ?? DateTime.MinValue;
    }

    public class StatisticCard
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
    }

    #endregion
}