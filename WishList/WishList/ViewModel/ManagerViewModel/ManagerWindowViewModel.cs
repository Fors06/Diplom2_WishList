using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WishList.Data.SwitchTheme;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;
using Task = WishList.Model.Entity.Task;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace WishList.ViewModel.ManagerViewModel
{
    #region Helper Classes

    public class ClientWithOrder : INotifyPropertyChanged
    {
        private Client _client;
        public Client Client
        {
            get => _client;
            set
            {
                _client = value;
                OnPropertyChanged(nameof(Client));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(CompanyName));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(ContactPerson));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(CreatedDate));
            }
        }
        public int OrderNumber { get; set; }
        public int Id => Client?.Id ?? 0;
        public string CompanyName => Client?.CompanyName ?? string.Empty;
        public string Email => Client?.Email ?? string.Empty;
        public string ContactPerson => Client?.ContactPerson ?? string.Empty;
        public string Phone => Client?.Phone ?? string.Empty;
        public string Address => Client?.Address ?? string.Empty;
        public DateTime CreatedDate => Client?.CreatedDate ?? DateTime.MinValue;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class StatisticCard : INotifyPropertyChanged
    {
        private string _icon;
        private string _title;
        private string _value;
        private string _description;
        private string _color;
        public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(nameof(Icon)); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }
        public string Value { get => _value; set { _value = value; OnPropertyChanged(nameof(Value)); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(nameof(Description)); } }
        public string Color { get => _color; set { _color = value; OnPropertyChanged(nameof(Color)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    } 

    public class FilterCheckItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isAll;
        public int Id { get; set; }
        public string Name { get; set; }

        public bool IsAll
        {
            get => _isAll;
            set
            {
                _isAll = value;
                OnPropertyChanged(nameof(IsAll));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class SortOption : INotifyPropertyChanged
    {
        private string _name;
        private string _field;
        private bool _isAscending;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public string Field { get => _field; set { _field = value; OnPropertyChanged(nameof(Field)); } }
        public bool IsAscending { get => _isAscending; set { _isAscending = value; OnPropertyChanged(nameof(IsAscending)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

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
        private int _selectedTabIndex;

        public ManagerWindowViewModel()
        {
            ExcelPackage.License.SetNonCommercialPersonal("WishList Application");

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
            StatusFilterItems = new ObservableCollection<FilterCheckItem>();
            PriorityFilterItems = new ObservableCollection<FilterCheckItem>();
            CategoryFilterItems = new ObservableCollection<FilterCheckItem>();

            CurrentClient = new Client();
            CurrentOrder = new Task();
            CurrentWorkPlan = new WorkPlan();

            InitializeFilterOptions();
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
        public ObservableCollection<FilterCheckItem> StatusFilterItems { get; }
        public ObservableCollection<FilterCheckItem> PriorityFilterItems { get; }
        public ObservableCollection<FilterCheckItem> CategoryFilterItems { get; }

        public string CurrentDate => DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        public string ClientSearchText
        {
            get => _clientSearchText;
            set
            {
                _clientSearchText = value;
                OnPropertyChanged(nameof(ClientSearchText));
                ApplyClientFilters();
            }
        }

        public string OrderSearchText
        {
            get => _orderSearchText;
            set
            {
                _orderSearchText = value;
                OnPropertyChanged(nameof(OrderSearchText));
                ApplyOrderFilters();
            }
        }

        public string WorkPlanSearchText
        {
            get => _workPlanSearchText;
            set
            {
                _workPlanSearchText = value;
                OnPropertyChanged(nameof(WorkPlanSearchText));
                ApplyWorkPlanFilters();
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
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        #endregion

        #region Filter and Sort Properties - Clients

        private ObservableCollection<SortOption> _clientSortOptions;
        public ObservableCollection<SortOption> ClientSortOptions
        {
            get => _clientSortOptions;
            set { _clientSortOptions = value; OnPropertyChanged(nameof(ClientSortOptions)); }
        }

        private SortOption _selectedClientSort;
        public SortOption SelectedClientSort
        {
            get => _selectedClientSort;
            set
            {
                _selectedClientSort = value;
                OnPropertyChanged(nameof(SelectedClientSort));
                ApplyClientSort();
            }
        }

        private DateTime? _clientFilterStartDate;
        public DateTime? ClientFilterStartDate
        {
            get => _clientFilterStartDate;
            set
            {
                if (_clientFilterStartDate != value)
                {
                    _clientFilterStartDate = value;
                    OnPropertyChanged(nameof(ClientFilterStartDate));
                    ApplyClientFilters();
                }
            }
        }

        private DateTime? _clientFilterEndDate;
        public DateTime? ClientFilterEndDate
        {
            get => _clientFilterEndDate;
            set
            {
                if (_clientFilterEndDate != value)
                {
                    _clientFilterEndDate = value;
                    OnPropertyChanged(nameof(ClientFilterEndDate));
                    ApplyClientFilters();
                }
            }
        }

        #endregion

        #region Filter and Sort Properties - Orders

        private ObservableCollection<SortOption> _orderSortOptions;
        public ObservableCollection<SortOption> OrderSortOptions
        {
            get => _orderSortOptions;
            set { _orderSortOptions = value; OnPropertyChanged(nameof(OrderSortOptions)); }
        }

        private SortOption _selectedOrderSort;
        public SortOption SelectedOrderSort
        {
            get => _selectedOrderSort;
            set
            {
                _selectedOrderSort = value;
                OnPropertyChanged(nameof(SelectedOrderSort));
                ApplyOrderSort();
            }
        }

        private DateTime? _orderFilterStartDate;
        public DateTime? OrderFilterStartDate
        {
            get => _orderFilterStartDate;
            set
            {
                if (_orderFilterStartDate != value)
                {
                    _orderFilterStartDate = value;
                    OnPropertyChanged(nameof(OrderFilterStartDate));
                    ApplyOrderFilters();
                }
            }
        }

        private DateTime? _orderFilterEndDate;
        public DateTime? OrderFilterEndDate
        {
            get => _orderFilterEndDate;
            set
            {
                if (_orderFilterEndDate != value)
                {
                    _orderFilterEndDate = value;
                    OnPropertyChanged(nameof(OrderFilterEndDate));
                    ApplyOrderFilters();
                }
            }
        }

        #endregion

        #region Filter and Sort Properties - WorkPlans

        private ObservableCollection<SortOption> _workPlanSortOptions;
        public ObservableCollection<SortOption> WorkPlanSortOptions
        {
            get => _workPlanSortOptions;
            set { _workPlanSortOptions = value; OnPropertyChanged(nameof(WorkPlanSortOptions)); }
        }

        private SortOption _selectedWorkPlanSort;
        public SortOption SelectedWorkPlanSort
        {
            get => _selectedWorkPlanSort;
            set
            {
                _selectedWorkPlanSort = value;
                OnPropertyChanged(nameof(SelectedWorkPlanSort));
                ApplyWorkPlanSort();
            }
        }

        private DateTime? _workPlanFilterStartDate;
        public DateTime? WorkPlanFilterStartDate
        {
            get => _workPlanFilterStartDate;
            set
            {
                if (_workPlanFilterStartDate != value)
                {
                    _workPlanFilterStartDate = value;
                    OnPropertyChanged(nameof(WorkPlanFilterStartDate));
                    ApplyWorkPlanFilters();
                }
            }
        }

        private DateTime? _workPlanFilterEndDate;
        public DateTime? WorkPlanFilterEndDate
        {
            get => _workPlanFilterEndDate;
            set
            {
                if (_workPlanFilterEndDate != value)
                {
                    _workPlanFilterEndDate = value;
                    OnPropertyChanged(nameof(WorkPlanFilterEndDate));
                    ApplyWorkPlanFilters();
                }
            }
        }

        #endregion

        #region Reset Filters Commands

        public ICommand ResetClientFiltersCommand { get; private set; }
        public ICommand ResetOrderFiltersCommand { get; private set; }
        public ICommand ResetWorkPlanFiltersCommand { get; private set; }

        #endregion

        #region Commands

        public ICommand ToggleThemeCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
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
        public ICommand ExportOrdersCommand { get; private set; }
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

        #endregion

        #region Initialize Methods

        private void InitializeFilterOptions()
        {
            ClientSortOptions = new ObservableCollection<SortOption>
            {
                new SortOption { Name = "По умолчанию", Field = "Id", IsAscending = true },
                new SortOption { Name = "По названию (А-Я)", Field = "CompanyName", IsAscending = true },
                new SortOption { Name = "По названию (Я-А)", Field = "CompanyName", IsAscending = false },
                new SortOption { Name = "По дате регистрации (новые)", Field = "CreatedDate", IsAscending = false },
                new SortOption { Name = "По дате регистрации (старые)", Field = "CreatedDate", IsAscending = true }
            };
            SelectedClientSort = ClientSortOptions[0];

            OrderSortOptions = new ObservableCollection<SortOption>
            {
                new SortOption { Name = "По умолчанию", Field = "Id", IsAscending = true },
                new SortOption { Name = "По дате создания (новые)", Field = "CreatedDate", IsAscending = false },
                new SortOption { Name = "По дате создания (старые)", Field = "CreatedDate", IsAscending = true },
                new SortOption { Name = "По сроку выполнения (ближайшие)", Field = "DueDate", IsAscending = true },
                new SortOption { Name = "По названию (А-Я)", Field = "Title", IsAscending = true },
                new SortOption { Name = "По приоритету (высокий)", Field = "PriorityId", IsAscending = false }
            };
            SelectedOrderSort = OrderSortOptions[0];

            WorkPlanSortOptions = new ObservableCollection<SortOption>
            {
                new SortOption { Name = "По умолчанию", Field = "Id", IsAscending = true },
                new SortOption { Name = "По дате создания (новые)", Field = "CreatedDate", IsAscending = false },
                new SortOption { Name = "По дате создания (старые)", Field = "CreatedDate", IsAscending = true },
                new SortOption { Name = "По часам (больше)", Field = "EstimatedHours", IsAscending = false },
                new SortOption { Name = "По описанию (А-Я)", Field = "PlanDescription", IsAscending = true }
            };
            SelectedWorkPlanSort = WorkPlanSortOptions[0];
        }

        private void InitializeCommands()
        {
            ToggleThemeCommand = new RelayCommand(_ => ExecuteToggleTheme());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
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
            ExportOrdersCommand = new RelayCommand(_ => ExecuteExportOrders());
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

            ResetClientFiltersCommand = new RelayCommand(_ => ResetClientFilters());
            ResetOrderFiltersCommand = new RelayCommand(_ => ResetOrderFilters());
            ResetWorkPlanFiltersCommand = new RelayCommand(_ => ResetWorkPlanFilters());
        }

        #endregion

        #region Reset Filters Methods

        private void ResetClientFilters()
        {
            ClientSearchText = string.Empty;
            ClientFilterStartDate = null;
            ClientFilterEndDate = null;
            SelectedClientSort = ClientSortOptions.FirstOrDefault();
            StatusMessage = "Фильтры клиентов сброшены";
        }

        private void ResetOrderFilters()
        {
            OrderSearchText = string.Empty;
            OrderFilterStartDate = null;
            OrderFilterEndDate = null;
            SelectedOrderSort = OrderSortOptions.FirstOrDefault();

            foreach (var item in StatusFilterItems)
            {
                item.IsSelected = item.IsAll;
            }
            foreach (var item in PriorityFilterItems)
            {
                item.IsSelected = item.IsAll;
            }
            foreach (var item in CategoryFilterItems)
            {
                item.IsSelected = item.IsAll;
            }

            StatusMessage = "Фильтры заказов сброшены";
        }

        private void ResetWorkPlanFilters()
        {
            WorkPlanSearchText = string.Empty;
            WorkPlanFilterStartDate = null;
            WorkPlanFilterEndDate = null;
            SelectedWorkPlanSort = WorkPlanSortOptions.FirstOrDefault();
            StatusMessage = "Фильтры планов работ сброшены";
        }

        #endregion

        #region Sort and Filter Methods

        private void ApplyClientSort()
        {
            if (SelectedClientSort == null || FilteredClients == null) return;

            var list = FilteredClients.ToList();

            switch (SelectedClientSort.Field)
            {
                case "Id":
                    list = list.OrderBy(c => c.Id).ToList();
                    break;
                case "CompanyName":
                    list = SelectedClientSort.IsAscending
                        ? list.OrderBy(c => c.CompanyName).ToList()
                        : list.OrderByDescending(c => c.CompanyName).ToList();
                    break;
                case "CreatedDate":
                    list = SelectedClientSort.IsAscending
                        ? list.OrderBy(c => c.CreatedDate).ToList()
                        : list.OrderByDescending(c => c.CreatedDate).ToList();
                    break;
            }

            FilteredClients.Clear();
            foreach (var item in list)
            {
                FilteredClients.Add(item);
            }
        }

        private void ApplyOrderSort()
        {
            if (SelectedOrderSort == null || FilteredOrders == null) return;

            var list = FilteredOrders.ToList();

            switch (SelectedOrderSort.Field)
            {
                case "Id":
                    list = list.OrderBy(o => o.Id).ToList();
                    break;
                case "Title":
                    list = SelectedOrderSort.IsAscending
                        ? list.OrderBy(o => o.Title).ToList()
                        : list.OrderByDescending(o => o.Title).ToList();
                    break;
                case "CreatedDate":
                    list = SelectedOrderSort.IsAscending
                        ? list.OrderBy(o => o.CreatedDate).ToList()
                        : list.OrderByDescending(o => o.CreatedDate).ToList();
                    break;
                case "DueDate":
                    list = SelectedOrderSort.IsAscending
                        ? list.OrderBy(o => o.Task.DueDate).ToList()
                        : list.OrderByDescending(o => o.Task.DueDate).ToList();
                    break;
                case "PriorityId":
                    list = SelectedOrderSort.IsAscending
                        ? list.OrderBy(o => o.Task.PriorityId).ToList()
                        : list.OrderByDescending(o => o.Task.PriorityId).ToList();
                    break;
            }

            FilteredOrders.Clear();
            foreach (var item in list)
            {
                FilteredOrders.Add(item);
            }
        }

        private void ApplyWorkPlanSort()
        {
            if (SelectedWorkPlanSort == null || FilteredWorkPlans == null) return;

            var list = FilteredWorkPlans.ToList();

            switch (SelectedWorkPlanSort.Field)
            {
                case "Id":
                    list = list.OrderBy(w => w.Id).ToList();
                    break;
                case "PlanDescription":
                    list = SelectedWorkPlanSort.IsAscending
                        ? list.OrderBy(w => w.PlanDescription).ToList()
                        : list.OrderByDescending(w => w.PlanDescription).ToList();
                    break;
                case "CreatedDate":
                    list = SelectedWorkPlanSort.IsAscending
                        ? list.OrderBy(w => w.CreatedDate).ToList()
                        : list.OrderByDescending(w => w.CreatedDate).ToList();
                    break;
                case "EstimatedHours":
                    list = SelectedWorkPlanSort.IsAscending
                        ? list.OrderBy(w => w.EstimatedHours).ToList()
                        : list.OrderByDescending(w => w.EstimatedHours).ToList();
                    break;
            }

            FilteredWorkPlans.Clear();
            foreach (var item in list)
            {
                FilteredWorkPlans.Add(item);
            }
        }

        private void ApplyClientFilters()
        {
            if (Clients == null) return;

            var filtered = Clients.AsEnumerable();

            if (ClientFilterStartDate.HasValue)
            {
                filtered = filtered.Where(c => c.CreatedDate.Date >= ClientFilterStartDate.Value.Date);
            }
            if (ClientFilterEndDate.HasValue)
            {
                filtered = filtered.Where(c => c.CreatedDate.Date <= ClientFilterEndDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(ClientSearchText))
            {
                var searchLower = ClientSearchText.ToLower();
                filtered = filtered.Where(c => c.CompanyName.ToLower().Contains(searchLower) ||
                                               c.Email.ToLower().Contains(searchLower));
            }

            FilteredClients.Clear();
            foreach (var client in filtered)
            {
                FilteredClients.Add(client);
            }

            ApplyClientSort();
        }

        private void ApplyOrderFilters()
        {
            if (Orders == null) return;

            var filtered = Orders.AsEnumerable();

            var allStatusSelected = StatusFilterItems.FirstOrDefault(s => s.IsAll)?.IsSelected ?? false;
            var selectedStatusIds = StatusFilterItems.Where(s => s.IsSelected && !s.IsAll).Select(s => s.Id).ToList();

            if (!allStatusSelected && selectedStatusIds.Any())
            {
                filtered = filtered.Where(o => o.Status != null && selectedStatusIds.Contains(o.Status.Id));
            }

            var allPrioritySelected = PriorityFilterItems.FirstOrDefault(p => p.IsAll)?.IsSelected ?? false;
            var selectedPriorityIds = PriorityFilterItems.Where(p => p.IsSelected && !p.IsAll).Select(p => p.Id).ToList();

            if (!allPrioritySelected && selectedPriorityIds.Any())
            {
                filtered = filtered.Where(o => o.Priority != null && selectedPriorityIds.Contains(o.Priority.Id));
            }

            var allCategorySelected = CategoryFilterItems.FirstOrDefault(c => c.IsAll)?.IsSelected ?? false;
            var selectedCategoryIds = CategoryFilterItems.Where(c => c.IsSelected && !c.IsAll).Select(c => c.Id).ToList();

            if (!allCategorySelected && selectedCategoryIds.Any())
            {
                filtered = filtered.Where(o => o.Category != null && selectedCategoryIds.Contains(o.Category.Id));
            }

            if (OrderFilterStartDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedDate.Date >= OrderFilterStartDate.Value.Date);
            }
            if (OrderFilterEndDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedDate.Date <= OrderFilterEndDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(OrderSearchText))
            {
                var searchLower = OrderSearchText.ToLower();
                filtered = filtered.Where(o => o.Title.ToLower().Contains(searchLower) ||
                                               (o.Client?.CompanyName ?? "").ToLower().Contains(searchLower));
            }

            FilteredOrders.Clear();
            foreach (var order in filtered)
            {
                FilteredOrders.Add(order);
            }

            ApplyOrderSort();
        }

        private void ApplyWorkPlanFilters()
        {
            if (WorkPlans == null) return;

            var filtered = WorkPlans.AsEnumerable();

            if (WorkPlanFilterStartDate.HasValue)
            {
                filtered = filtered.Where(w => w.CreatedDate.Date >= WorkPlanFilterStartDate.Value.Date);
            }
            if (WorkPlanFilterEndDate.HasValue)
            {
                filtered = filtered.Where(w => w.CreatedDate.Date <= WorkPlanFilterEndDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(WorkPlanSearchText))
            {
                var searchLower = WorkPlanSearchText.ToLower();
                filtered = filtered.Where(w => w.PlanDescription.ToLower().Contains(searchLower));
            }

            FilteredWorkPlans.Clear();
            foreach (var plan in filtered)
            {
                FilteredWorkPlans.Add(plan);
            }

            ApplyWorkPlanSort();
        }

        private void SyncFilterSelection(ObservableCollection<FilterCheckItem> items, FilterCheckItem changedItem)
        {
            if (changedItem.IsAll && changedItem.IsSelected)
            {
                foreach (var item in items.Where(x => !x.IsAll))
                {
                    item.IsSelected = false;
                }
            }
            else if (!changedItem.IsAll && changedItem.IsSelected)
            {
                var allItem = items.FirstOrDefault(x => x.IsAll);
                if (allItem != null && allItem.IsSelected)
                {
                    allItem.IsSelected = false;
                }
            }
        }

        #endregion

        #region Command Methods

        private void ExecuteToggleTheme()
        {
            ThemeManager.ToggleTheme();
            StatusMessage = "Тема изменена";
        }

        private void ExecuteLogout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.MainWindow.Close();
                var loginWindow = new Views.MainWindow();
                loginWindow.Show();
                Application.Current.MainWindow = loginWindow;
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
                foreach (var client in clients)
                {
                    Clients.Add(new ClientWithOrder { Client = client, OrderNumber = orderNumber++ });
                }
                ApplyClientFilters();
                StatusMessage = $"Загружено {Clients.Count} клиентов";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
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
                    .Include(t => t.TaskProgress)
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                Orders.Clear();
                int orderNumber = 1;
                foreach (var order in orders)
                {
                    Orders.Add(new TaskWithOrder { Task = order, OrderNumber = orderNumber++ });
                }
                ApplyOrderFilters();
                StatusMessage = $"Загружено {Orders.Count} заказов";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
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
                var workPlans = _workPlansRepository.GetAll().OrderByDescending(wp => wp.CreatedDate).ToList();
                WorkPlans.Clear();
                int orderNumber = 1;
                foreach (var workPlan in workPlans)
                {
                    WorkPlans.Add(new WorkPlanWithOrder { WorkPlan = workPlan, OrderNumber = orderNumber++ });
                }
                ApplyWorkPlanFilters();
                StatusMessage = $"Загружено {WorkPlans.Count} планов";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
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
                AllClients.Clear();
                foreach (var c in _clientsRepository.GetAll().OrderBy(c => c.CompanyName))
                    AllClients.Add(c);

                AllCategories.Clear();
                foreach (var c in _categoriesRepository.GetAll().OrderBy(c => c.Name))
                    AllCategories.Add(c);

                AllPriorities.Clear();
                foreach (var p in _prioritiesRepository.GetAll().OrderBy(p => p.Id))
                    AllPriorities.Add(p);

                AllStatuses.Clear();
                foreach (var s in _statusesRepository.GetAll().OrderBy(s => s.Id))
                    AllStatuses.Add(s);

                AllManagers.Clear();
                var managers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role != null && e.Role.Name == "Manager")
                    .OrderBy(e => e.LastName)
                    .ToList();
                foreach (var m in managers)
                    AllManagers.Add(m);

                AllProgrammers.Clear();
                var programmers = _employeesRepository.GetAll()
                    .Where(e => e.IsActive && e.Role != null && e.Role.Name == "Programmer")
                    .OrderBy(e => e.LastName)
                    .ToList();
                foreach (var p in programmers)
                    AllProgrammers.Add(p);

                StatusFilterItems.Clear();
                var allStatus = new FilterCheckItem { Id = 0, Name = "Все", IsSelected = true, IsAll = true };
                StatusFilterItems.Add(allStatus);
                var statuses = _statusesRepository.GetAll().ToList();
                foreach (var s in statuses)
                {
                    StatusFilterItems.Add(new FilterCheckItem { Id = s.Id, Name = s.Name, IsSelected = false });
                }

                foreach (var item in StatusFilterItems)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FilterCheckItem.IsSelected))
                        {
                            SyncFilterSelection(StatusFilterItems, (FilterCheckItem)s);
                            ApplyOrderFilters();
                        }
                    };
                }

                PriorityFilterItems.Clear();
                var allPriority = new FilterCheckItem { Id = 0, Name = "Все", IsSelected = true, IsAll = true };
                PriorityFilterItems.Add(allPriority);
                var priorities = _prioritiesRepository.GetAll().ToList();
                foreach (var p in priorities)
                {
                    PriorityFilterItems.Add(new FilterCheckItem { Id = p.Id, Name = p.Name, IsSelected = false });
                }

                foreach (var item in PriorityFilterItems)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FilterCheckItem.IsSelected))
                        {
                            SyncFilterSelection(PriorityFilterItems, (FilterCheckItem)s);
                            ApplyOrderFilters();
                        }
                    };
                }

                CategoryFilterItems.Clear();
                var allCategory = new FilterCheckItem { Id = 0, Name = "Все", IsSelected = true, IsAll = true };
                CategoryFilterItems.Add(allCategory);
                var categories = _categoriesRepository.GetAll().ToList();
                foreach (var c in categories)
                {
                    CategoryFilterItems.Add(new FilterCheckItem { Id = c.Id, Name = c.Name, IsSelected = false });
                }

                foreach (var item in CategoryFilterItems)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(FilterCheckItem.IsSelected))
                        {
                            SyncFilterSelection(CategoryFilterItems, (FilterCheckItem)s);
                            ApplyOrderFilters();
                        }
                    };
                }

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

        public void AddClient()
        {
            IsClientFormVisible = true;
            IsEditingClient = false;
            CurrentClient = new Client { CreatedDate = DateTime.Now };
            StatusMessage = "Добавление клиента";
        }

        private void EditClient(object parameter)
        {
            if (parameter is int id)
            {
                var client = _clientsRepository.GetById(id);
                if (client != null)
                {
                    IsClientFormVisible = true;
                    IsEditingClient = true;
                    CurrentClient = client;
                    StatusMessage = $"Редактирование: {client.CompanyName}";
                }
            }
        }

        private void DeleteClient(object parameter)
        {
            if (parameter is int id && MessageBox.Show("Удалить клиента?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _clientsRepository.Delete(id);
                    _clientsRepository.Save();
                    LoadClients();
                    LoadComboBoxData();
                    StatusMessage = "Клиент удален";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        public void SaveClient()
        {
            if (string.IsNullOrWhiteSpace(CurrentClient.CompanyName))
            {
                MessageBox.Show("Введите название", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(CurrentClient.Email))
            {
                MessageBox.Show("Введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                if (IsEditingClient)
                    _clientsRepository.Update(CurrentClient);
                else
                    _clientsRepository.Create(CurrentClient);
                _clientsRepository.Save();
                IsClientFormVisible = false;
                LoadClients();
                LoadComboBoxData();
                MessageBox.Show(IsEditingClient ? "Клиент обновлен" : "Клиент создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        public void CancelClient()
        {
            IsClientFormVisible = false;
            CurrentClient = new Client();
            StatusMessage = "Отменено";
        }

        public void RefreshClients()
        {
            LoadClients();
        }

        #endregion

        #region Order Methods

        public void AddOrder()
        {
            IsOrderFormVisible = true;
            IsEditingOrder = false;
            CurrentOrder = new Task
            {
                CreatedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7),
                StatusId = 1,
                PriorityId = 2,
                ManagerId = GetCurrentManagerId(),
                EstimatedHours = 8,
                ActualHours = 0
            };
            StatusMessage = "Создание заказа";
        }

        private void EditOrder(object parameter)
        {
            if (parameter is int id)
            {
                var order = _tasksRepository.GetAll()
                    .Include(t => t.Client)
                    .Include(t => t.Category)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .Include(t => t.Manager)
                    .Include(t => t.Programmer)
                    .FirstOrDefault(t => t.Id == id);

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
                try
                {
                    _tasksRepository.Delete(id);
                    _tasksRepository.Save();
                    LoadOrders();
                    StatusMessage = "Заказ удален";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                }
            }
        }

        public void SaveOrder()
        {
            if (string.IsNullOrWhiteSpace(CurrentOrder.Title))
            {
                MessageBox.Show("Введите название", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.ClientId == 0)
            {
                MessageBox.Show("Выберите клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.CategoryId == 0)
            {
                MessageBox.Show("Выберите категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.PriorityId == 0)
            {
                MessageBox.Show("Выберите приоритет", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.StatusId == 0)
            {
                MessageBox.Show("Выберите статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.ManagerId == 0)
            {
                MessageBox.Show("Выберите менеджера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentOrder.EstimatedHours <= 0)
            {
                MessageBox.Show("Введите часы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditingOrder)
                {
                    var existing = _tasksRepository.GetById(CurrentOrder.Id);
                    if (existing != null)
                    {
                        existing.Title = CurrentOrder.Title;
                        existing.Description = CurrentOrder.Description;
                        existing.ClientId = CurrentOrder.ClientId;
                        existing.CategoryId = CurrentOrder.CategoryId;
                        existing.PriorityId = CurrentOrder.PriorityId;
                        existing.StatusId = CurrentOrder.StatusId;
                        existing.ManagerId = CurrentOrder.ManagerId;
                        existing.ProgrammerId = CurrentOrder.ProgrammerId;
                        existing.DueDate = CurrentOrder.DueDate;
                        existing.CompletedDate = CurrentOrder.CompletedDate;
                        existing.EstimatedHours = CurrentOrder.EstimatedHours;
                        existing.ActualHours = CurrentOrder.ActualHours;
                        _tasksRepository.Update(existing);
                        StatusMessage = "Заказ обновлен";
                    }
                }
                else
                {
                    var progress = new TaskProgress
                    {
                        ProgressPercentage = 0,
                        CreatedDate = DateTime.Now,
                        Description = "Начало работы"
                    };
                    _progressRepository.Create(progress);
                    _progressRepository.Save();
                    CurrentOrder.TaskProgressId = progress.Id;
                    CurrentOrder.CreatedDate = DateTime.Now;
                    _tasksRepository.Create(CurrentOrder);
                    StatusMessage = "Заказ создан";
                }
                _tasksRepository.Save();
                IsOrderFormVisible = false;
                LoadOrders();
                MessageBox.Show(IsEditingOrder ? "Заказ обновлен" : "Заказ создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelOrder()
        {
            IsOrderFormVisible = false;
            CurrentOrder = new Task();
            StatusMessage = "Отменено";
        }

        public void RefreshOrders()
        {
            LoadOrders();
        }

        #endregion

        #region WorkPlan Methods

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
                    try
                    {
                        _workPlansRepository.Delete(workPlanId);
                        _workPlansRepository.Save();
                        LoadWorkPlans();
                        StatusMessage = "План удален";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Ошибка: {ex.Message}";
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public void SaveWorkPlan()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription))
            {
                MessageBox.Show("Введите описание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentWorkPlan.EstimatedHours <= 0)
            {
                MessageBox.Show("Часы > 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                if (IsEditingWorkPlan)
                    _workPlansRepository.Update(CurrentWorkPlan);
                else
                    _workPlansRepository.Create(CurrentWorkPlan);
                _workPlansRepository.Save();
                IsWorkPlanFormVisible = false;
                LoadWorkPlans();
                MessageBox.Show(IsEditingWorkPlan ? "План обновлен" : "План создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelWorkPlan()
        {
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Отменено";
        }

        public void RefreshWorkPlans()
        {
            LoadWorkPlans();
        }

        private void ShowWorkPlan()
        {
            if (SelectedOrder == null)
            {
                MessageBox.Show("Выберите заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            LoadWorkPlanForSelectedOrder();
            IsWorkPlanDialogOpen = true;
            StatusMessage = $"Просмотр плана для: {SelectedOrder.Title}";
        }

        private void CloseWorkPlanDialog()
        {
            IsWorkPlanDialogOpen = false;
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Диалог закрыт";
        }

        private void AddWorkPlanInDialog()
        {
            IsWorkPlanFormVisible = true;
            IsEditingWorkPlan = false;
            CurrentWorkPlan = new WorkPlan { CreatedDate = DateTime.Now, EstimatedHours = 0 };
            StatusMessage = "Создание плана";
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
                    StatusMessage = $"Редактирование: {workPlan.PlanDescription}";
                }
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
                    foreach (var link in links)
                        _taskWorkPlansRepository.Delete(link.Id);
                    _workPlansRepository.Delete(workPlanId);
                    _workPlansRepository.Save();
                    LoadWorkPlanForSelectedOrder();
                    StatusMessage = "План удален";
                    MessageBox.Show("План удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveWorkPlanInDialog()
        {
            if (string.IsNullOrWhiteSpace(CurrentWorkPlan.PlanDescription))
            {
                MessageBox.Show("Введите описание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CurrentWorkPlan.EstimatedHours <= 0)
            {
                MessageBox.Show("Часы > 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                        StatusMessage = "План обновлен";
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

                    var link = new TaskWorkPlan
                    {
                        TaskId = SelectedOrder.Id,
                        WorkPlanId = newPlan.Id,
                        CreatedDate = DateTime.Now
                    };
                    _taskWorkPlansRepository.Create(link);
                    _taskWorkPlansRepository.Save();
                    StatusMessage = "Новый план создан и привязан к заказу";
                }
                IsWorkPlanFormVisible = false;
                LoadWorkPlanForSelectedOrder();
                MessageBox.Show(IsEditingWorkPlan ? "План обновлен" : "План создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CancelWorkPlanInDialog()
        {
            IsWorkPlanFormVisible = false;
            CurrentWorkPlan = new WorkPlan();
            StatusMessage = "Отменено";
        }

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
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export to Excel

        private void ExecuteExportOrders()
        {
            try
            {
                if (FilteredOrders == null || FilteredOrders.Count == 0)
                {
                    MessageBox.Show("Нет данных для экспорта", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.DefaultExt = "xlsx";
                saveDialog.FileName = $"Заказы_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                saveDialog.Title = "Сохранить отчёт";

                if (saveDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Заказы");

                        string[] headers = {
                            "№", "Название", "Описание", "Клиент", "Контактное лицо",
                            "Телефон клиента", "Email клиента", "Категория", "Приоритет", "Статус",
                            "Менеджер", "Программист", "Дата создания", "Срок выполнения",
                            "Дата завершения"
                        };

                        using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                        }

                        int row = 2;
                        int number = 1;
                        foreach (var order in FilteredOrders)
                        {
                            worksheet.Cells[row, 1].Value = number++;
                            worksheet.Cells[row, 2].Value = order.Title ?? "";
                            worksheet.Cells[row, 3].Value = order.Description ?? "";
                            worksheet.Cells[row, 4].Value = order.Client?.CompanyName ?? "";
                            worksheet.Cells[row, 5].Value = order.Client?.ContactPerson ?? "";
                            worksheet.Cells[row, 6].Value = order.Client?.Phone ?? "";
                            worksheet.Cells[row, 7].Value = order.Client?.Email ?? "";
                            worksheet.Cells[row, 8].Value = order.Category?.Name ?? "";
                            worksheet.Cells[row, 9].Value = order.Priority?.Name ?? "";
                            worksheet.Cells[row, 10].Value = order.Status?.Name ?? "";
                            worksheet.Cells[row, 11].Value = order.Manager?.Name ?? "";
                            worksheet.Cells[row, 12].Value = order.Programmer?.Name ?? "";
                            worksheet.Cells[row, 13].Value = order.CreatedDate.ToString("dd.MM.yyyy HH:mm");
                            worksheet.Cells[row, 14].Value = order.DueDate?.ToString("dd.MM.yyyy") ?? "";
                            worksheet.Cells[row, 15].Value = order.Task?.CompletedDate?.ToString("dd.MM.yyyy") ?? "";

                            row++;
                        }

                        worksheet.Cells[1, 1, row - 1, headers.Length].AutoFitColumns();
                        package.SaveAs(new FileInfo(saveDialog.FileName));
                    }

                    MessageBox.Show($"Экспорт завершён. Сохранено {FilteredOrders.Count} записей.\n\nФайл сохранён: {saveDialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage = $"Экспортировано {FilteredOrders.Count} заказов";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private int GetCurrentManagerId()
        {
            return _employeesRepository.GetAll().FirstOrDefault(e => e.IsActive && e.Role.Name == "Manager")?.Id ?? 2;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}