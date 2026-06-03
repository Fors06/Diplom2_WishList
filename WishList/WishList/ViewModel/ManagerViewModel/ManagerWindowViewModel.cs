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
        private readonly TaskWorkPlansRepository _taskWorkPlansRepository;

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
            _taskWorkPlansRepository = new TaskWorkPlansRepository(_context);

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
                OnPropertyChanged(nameof(ClientSearchText)); FilterClients();
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
        { get => _statusMessage; 
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
                if (value != null) LoadWorkPlanForSelectedOrder();
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
        public int SelectedTabIndex
        { 
            get => _selectedTabIndex; 
            set 
            {
                _selectedTabIndex = value; OnPropertyChanged(nameof(SelectedTabIndex)); 
            }
        }
        private int _selectedTabIndex;
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
            CurrentWorkPlan = new WorkPlan { CreatedDate = DateTime.Now, EstimatedHours = 0 };
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
                if (workPlan != null && MessageBox.Show($"Удалить план \"{workPlan.PlanDescription}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try { _workPlansRepository.Delete(workPlanId); _workPlansRepository.Save(); LoadWorkPlans(); StatusMessage = "План удален"; }
                    catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        public void SaveWorkPlan()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription)) { MessageBox.Show("Введите описание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentWorkPlan.EstimatedHours <= 0) { MessageBox.Show("Часы > 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                if (IsEditingWorkPlan) _workPlansRepository.Update(CurrentWorkPlan);
                else _workPlansRepository.Create(CurrentWorkPlan);
                _workPlansRepository.Save();
                IsWorkPlanFormVisible = false;
                LoadWorkPlans();
                MessageBox.Show(IsEditingWorkPlan ? "План обновлен" : "План создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void CancelWorkPlan() { IsWorkPlanFormVisible = false; CurrentWorkPlan = new WorkPlan(); StatusMessage = "Отменено"; }
        public void RefreshWorkPlans() { LoadWorkPlans(); }

        private void ShowWorkPlan()
        {
            if (SelectedOrder == null) { MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            LoadWorkPlanForSelectedOrder();
            IsWorkPlanDialogOpen = true;
            StatusMessage = $"Просмотр плана для: {SelectedOrder.Title}";
        }

        private void CloseWorkPlanDialog() { IsWorkPlanDialogOpen = false; IsWorkPlanFormVisible = false; CurrentWorkPlan = new WorkPlan(); StatusMessage = "Диалог закрыт"; }
        private void AddWorkPlanInDialog() { IsWorkPlanFormVisible = true; IsEditingWorkPlan = false; CurrentWorkPlan = new WorkPlan { CreatedDate = DateTime.Now, EstimatedHours = 0 }; StatusMessage = "Создание плана"; }

        private void EditWorkPlanInDialog(object parameter)
        {
            if (parameter is int workPlanId)
            {
                var workPlan = _workPlansRepository.GetById(workPlanId);
                if (workPlan != null) { IsWorkPlanFormVisible = true; IsEditingWorkPlan = true; CurrentWorkPlan = workPlan; StatusMessage = $"Редактирование: {workPlan.PlanDescription}"; }
            }
        }

        private void DeleteWorkPlanInDialog(object parameter)
        {
            if (parameter is int workPlanId)
            {
                var workPlan = _workPlansRepository.GetById(workPlanId);
                if (workPlan == null) return;
                if (MessageBox.Show($"Удалить план \"{workPlan.PlanDescription}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

                try
                {
                    var links = _taskWorkPlansRepository.Find(twp => twp.WorkPlanId == workPlanId).ToList();
                    foreach (var link in links) _taskWorkPlansRepository.Delete(link.Id);
                    _workPlansRepository.Delete(workPlanId);
                    _workPlansRepository.Save();
                    LoadWorkPlanForSelectedOrder();
                    StatusMessage = "План удален";
                    MessageBox.Show("План удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public void SaveWorkPlanInDialog()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription)) { MessageBox.Show("Введите описание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentWorkPlan.EstimatedHours <= 0) { MessageBox.Show("Часы > 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                if (IsEditingWorkPlan)
                {
                    var existingPlan = _workPlansRepository.GetById(CurrentWorkPlan.Id);
                    if (existingPlan != null)
                    {
                        existingPlan.PlanDescription = CurrentWorkPlan.PlanDescription;
                        existingPlan.TestSteps = CurrentWorkPlan.TestSteps;
                        existingPlan.EstimatedHours = CurrentWorkPlan.EstimatedHours;
                        _workPlansRepository.Update(existingPlan);
                        StatusMessage = $"План обновлен";
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

                    var link = new TaskWorkPlan { TaskId = SelectedOrder.Id, WorkPlanId = newPlan.Id, CreatedDate = DateTime.Now };
                    _taskWorkPlansRepository.Create(link);
                    _taskWorkPlansRepository.Save();
                    StatusMessage = $"Новый план создан и привязан к заказу";
                }
                IsWorkPlanFormVisible = false;
                LoadWorkPlanForSelectedOrder();
                MessageBox.Show(IsEditingWorkPlan ? "План обновлен" : "План создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void CancelWorkPlanInDialog() { IsWorkPlanFormVisible = false; CurrentWorkPlan = new WorkPlan(); StatusMessage = "Отменено"; }

        private void LoadWorkPlanForSelectedOrder()
        {
            if (SelectedOrder == null) return;
            try
            {
                WorkPlansForDialog.Clear();
                var links = _taskWorkPlansRepository.Find(twp => twp.TaskId == SelectedOrder.Id).ToList();
                int orderNumber = 1;
                foreach (var link in links)
                {
                    var workPlan = _workPlansRepository.GetById(link.WorkPlanId);
                    if (workPlan != null)
                    {
                        WorkPlansForDialog.Add(new WorkPlanWithOrder { WorkPlan = workPlan, OrderNumber = orderNumber++ });
                    }
                }
                StatusMessage = WorkPlansForDialog.Any() ? $"Загружено {WorkPlansForDialog.Count} планов" : "Нет планов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExecuteToggleTheme() { ThemeManager.ToggleTheme(); StatusMessage = $"Тема изменена"; }
        private void RefreshData() { LoadData(); UpdateStatistics(); StatusMessage = "Данные обновлены"; }
        public void LoadData() { LoadClients(); LoadOrders(); LoadWorkPlans(); LoadComboBoxData(); }
        #endregion

        #region Load Methods
        private void LoadClients()
        {
            try
            {
                IsLoading = true;
                var clients = _clientsRepository.GetAll().OrderBy(c => c.CompanyName).ToList();
                Clients.Clear();
                int orderNumber = 1;
                foreach (var client in clients) Clients.Add(new ClientWithOrder { Client = client, OrderNumber = orderNumber++ });
                FilterClients();
                StatusMessage = $"Загружено {Clients.Count} клиентов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private void LoadOrders()
        {
            try
            {
                IsLoading = true;
                var orders = _tasksRepository.GetAll()
                    .Include(t => t.Client).Include(t => t.Category).Include(t => t.Priority)
                    .Include(t => t.Status).Include(t => t.Manager).Include(t => t.Programmer)
                    .OrderByDescending(t => t.CreatedDate).ToList();

                Orders.Clear();
                int orderNumber = 1;
                foreach (var order in orders)
                {
                    Orders.Add(new TaskWithOrder { Task = order, OrderNumber = orderNumber++ });
                }
                FilterOrders();
                StatusMessage = $"Загружено {Orders.Count} заказов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private void LoadWorkPlans()
        {
            try
            {
                IsLoading = true;
                var workPlans = _workPlansRepository.GetAll().OrderByDescending(wp => wp.CreatedDate).ToList();
                WorkPlans.Clear();
                int orderNumber = 1;
                foreach (var workPlan in workPlans) WorkPlans.Add(new WorkPlanWithOrder { WorkPlan = workPlan, OrderNumber = orderNumber++ });
                FilterWorkPlans();
                StatusMessage = $"Загружено {WorkPlans.Count} планов";
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            finally { IsLoading = false; }
        }

        private void LoadComboBoxData()
        {
            try
            {
                // Загрузка клиентов
                AllClients.Clear();
                foreach (var c in _clientsRepository.GetAll().OrderBy(c => c.CompanyName))
                    AllClients.Add(c);

                // Загрузка категорий
                AllCategories.Clear();
                foreach (var c in _categoriesRepository.GetAll().OrderBy(c => c.Name))
                    AllCategories.Add(c);

                // Загрузка приоритетов
                AllPriorities.Clear();
                foreach (var p in _prioritiesRepository.GetAll().OrderBy(p => p.Id))
                    AllPriorities.Add(p);

                // Загрузка статусов
                AllStatuses.Clear();
                foreach (var s in _statusesRepository.GetAll().OrderBy(s => s.Id))
                    AllStatuses.Add(s);

                // Загрузка менеджеров (роль Manager) - ИСПРАВЛЕНО
                AllManagers.Clear();
                var managers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role != null && e.Role.Name == "Manager")
                    .OrderBy(e => e.LastName)
                    .ToList();
                foreach (var m in managers)
                    AllManagers.Add(m);

                // Загрузка программистов (роль Programmer) - ИСПРАВЛЕНО
                AllProgrammers.Clear();
                var programmers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role != null && e.Role.Name == "Programmer")
                    .OrderBy(e => e.LastName)
                    .ToList();
                foreach (var p in programmers)
                    AllProgrammers.Add(p);

                StatusMessage = $"Загружено менеджеров: {AllManagers.Count}, программистов: {AllProgrammers.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки данных: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Client Methods
        public void AddClient() { IsClientFormVisible = true; IsEditingClient = false; CurrentClient = new Client { CreatedDate = DateTime.Now }; StatusMessage = "Добавление клиента"; }
        private void EditClient(object parameter) { if (parameter is int id) { var client = _clientsRepository.GetById(id); if (client != null) { IsClientFormVisible = true; IsEditingClient = true; CurrentClient = client; StatusMessage = $"Редактирование: {client.CompanyName}"; } } }
        private void DeleteClient(object parameter) { if (parameter is int id && MessageBox.Show("Удалить клиента?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { try { _clientsRepository.Delete(id); _clientsRepository.Save(); LoadClients(); LoadComboBoxData(); StatusMessage = "Клиент удален"; } catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; } } }
        public void SaveClient() { if (string.IsNullOrWhiteSpace(CurrentClient.CompanyName)) { MessageBox.Show("Введите название", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; } if (string.IsNullOrWhiteSpace(CurrentClient.Email)) { MessageBox.Show("Введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; } try { if (IsEditingClient) _clientsRepository.Update(CurrentClient); else _clientsRepository.Create(CurrentClient); _clientsRepository.Save(); IsClientFormVisible = false; LoadClients(); LoadComboBoxData(); MessageBox.Show(IsEditingClient ? "Клиент обновлен" : "Клиент создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; } }
        public void CancelClient() { IsClientFormVisible = false; CurrentClient = new Client(); StatusMessage = "Отменено"; }
        #endregion

        #region Order Methods
        public void AddOrder()
        {
            IsOrderFormVisible = true;
            IsEditingOrder = false;
            CurrentOrder = new Task { CreatedDate = DateTime.Now, DueDate = DateTime.Now.AddDays(7), StatusId = 1, PriorityId = 2, ManagerId = GetCurrentManagerId(), EstimatedHours = 8, ActualHours = 0 };
            StatusMessage = "Создание заказа";
        }

        private void EditOrder(object parameter)
        {
            if (parameter is int id)
            {
                var order = _tasksRepository.GetAll().Include(t => t.Client).Include(t => t.Category).Include(t => t.Priority).Include(t => t.Status).Include(t => t.Manager).Include(t => t.Programmer).FirstOrDefault(t => t.Id == id);
                if (order != null)
                {
                    IsOrderFormVisible = true;
                    IsEditingOrder = true;
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
                        TaskProgressId = order.TaskProgressId
                    };
                    StatusMessage = $"Редактирование: {order.Title}";
                }
            }
        }

        private void DeleteOrder(object parameter)
        {
            if (parameter is int id && MessageBox.Show("Удалить заказ?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try { _tasksRepository.Delete(id); _tasksRepository.Save(); LoadOrders(); StatusMessage = "Заказ удален"; }
                catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
            }
        }

        public void SaveOrder()
        {
            if (string.IsNullOrWhiteSpace(CurrentOrder.Title)) { MessageBox.Show("Введите название", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.ClientId == 0) { MessageBox.Show("Выберите клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.CategoryId == 0) { MessageBox.Show("Выберите категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.PriorityId == 0) { MessageBox.Show("Выберите приоритет", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.StatusId == 0) { MessageBox.Show("Выберите статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.ManagerId == 0) { MessageBox.Show("Выберите менеджера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CurrentOrder.EstimatedHours <= 0) { MessageBox.Show("Введите часы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                if (IsEditingOrder)
                {
                    var existing = _tasksRepository.GetById(CurrentOrder.Id);
                    if (existing != null)
                    {
                        existing.Title = CurrentOrder.Title; existing.Description = CurrentOrder.Description;
                        existing.ClientId = CurrentOrder.ClientId; existing.CategoryId = CurrentOrder.CategoryId;
                        existing.PriorityId = CurrentOrder.PriorityId; existing.StatusId = CurrentOrder.StatusId;
                        existing.ManagerId = CurrentOrder.ManagerId; existing.ProgrammerId = CurrentOrder.ProgrammerId;
                        existing.DueDate = CurrentOrder.DueDate; existing.CompletedDate = CurrentOrder.CompletedDate;
                        existing.EstimatedHours = CurrentOrder.EstimatedHours; existing.ActualHours = CurrentOrder.ActualHours;
                        _tasksRepository.Update(existing);
                        StatusMessage = $"Заказ обновлен";
                    }
                }
                else
                {
                    var progress = new TaskProgress { ProgressPercentage = 0, CreatedDate = DateTime.Now };
                    _progressRepository.Create(progress); _progressRepository.Save();
                    CurrentOrder.TaskProgressId = progress.Id;
                    CurrentOrder.CreatedDate = DateTime.Now;
                    _tasksRepository.Create(CurrentOrder);
                    StatusMessage = $"Заказ создан";
                }
                _tasksRepository.Save();
                IsOrderFormVisible = false;
                LoadOrders();
                MessageBox.Show(IsEditingOrder ? "Заказ обновлен" : "Заказ создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void CancelOrder() { IsOrderFormVisible = false; CurrentOrder = new Task(); StatusMessage = "Отменено"; }
        public void RefreshClients() { LoadClients(); }
        public void RefreshOrders() { LoadOrders(); }
        #endregion

        #region Filter Methods
        private void FilterClients() 
        {
            FilteredClients.Clear();
            var filtered = string.IsNullOrEmpty(ClientSearchText) ? Clients : Clients.
                Where(c => c.CompanyName.
                Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase)); 
            foreach (var c in filtered) FilteredClients.Add(c);
        }
        private void FilterOrders() 
        { 
            FilteredOrders.Clear(); 
            var filtered = string.IsNullOrEmpty(OrderSearchText) ? Orders : Orders.
                Where(o => o.Title.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase) ||
                o.Client.CompanyName.Contains(OrderSearchText, StringComparison.OrdinalIgnoreCase)); 
            foreach (var o in filtered) FilteredOrders.Add(o);
        }
        private void FilterWorkPlans() 
        {
            FilteredWorkPlans.Clear(); 
            var filtered = string.IsNullOrEmpty(WorkPlanSearchText) ? WorkPlans : WorkPlans.
                Where(w => w.PlanDescription.Contains(WorkPlanSearchText, StringComparison.OrdinalIgnoreCase)); 
            foreach (var w in filtered) FilteredWorkPlans.Add(w); 
        }
        #endregion

        #region Statistics
        private void UpdateStatistics()
        {
            StatisticsCards.Clear();
            StatisticsCards.Add(new StatisticCard { Icon = "👥", Title = "Клиенты", Value = Clients.Count.ToString(), Description = "Всего клиентов", Color = "#3498DB" });
            StatisticsCards.Add(new StatisticCard { Icon = "📦", Title = "Заказы", Value = Orders.Count.ToString(), Description = "Всего заказов", Color = "#2ECC71" });
            StatisticsCards.Add(new StatisticCard { Icon = "⚡", Title = "Активные", Value = Orders.Count(o => o.Status?.Name != "Completed").ToString(), Description = "Активных заказов", Color = "#F39C12" });
            StatisticsCards.Add(new StatisticCard { Icon = "📋", Title = "Планы", Value = WorkPlans.Count.ToString(), Description = "Планов работ", Color = "#9B59B6" });
        }
        #endregion

        #region Helper Methods
        private int GetCurrentManagerId() => _employeesRepository.GetAll().FirstOrDefault(e => e.IsActive && e.Role.Name == "Manager")?.Id ?? 2;
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Helper Classes
    public class ClientWithOrder 
    {
        public Client Client { get; set; } 
        public int OrderNumber { get; set; } 
        public int Id => Client?.Id ?? 0; 
        public string CompanyName => Client?.CompanyName ?? string.Empty;
        public string Email => Client?.Email ?? string.Empty;
        public string ContactPerson => Client?.ContactPerson ?? string.Empty; 
        public string Phone => Client?.Phone ?? string.Empty; 
        public string Address => Client?.Address ?? string.Empty; 
        public DateTime CreatedDate => Client?.CreatedDate ?? DateTime.MinValue; 
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
        public string Color { get; set; } }
    #endregion
}