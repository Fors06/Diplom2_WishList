using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;

namespace WishList.ViewModel.AdminViewModel.Dop
{
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
                OnPropertyChanged(nameof(ContactPerson));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Address));
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public int OrderNumber { get; set; }

        // Прокси-свойства для привязки
        public int Id => Client?.Id ?? 0;
        public string CompanyName => Client?.CompanyName ?? string.Empty;
        public string ContactPerson => Client?.ContactPerson ?? string.Empty;
        public string Email => Client?.Email ?? string.Empty;
        public string Phone => Client?.Phone ?? string.Empty;
        public string Address => Client?.Address ?? string.Empty;
        public DateTime CreatedDate => Client?.CreatedDate ?? DateTime.MinValue;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ClientsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly ClientsRepository _clientsRepository;

        // Константы для диапазона дат
        private readonly DateTime _minDate = DateTime.Today.AddYears(-1);
        private readonly DateTime _maxDate = DateTime.Today.AddYears(1);
        private const double TrackWidth = 400;

        public ClientsViewModel()
        {
            _context = new ApplicationContext();
            _clientsRepository = new ClientsRepository(_context);

            Clients = new ObservableCollection<ClientWithOrder>();
            FilteredClients = new ObservableCollection<ClientWithOrder>();

            // Инициализация свойств для слайдера
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            UpdateSliderProperties();

            ClientsView = CollectionViewSource.GetDefaultView(FilteredClients);
            ClientsView.Filter = FilterClients;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties

        private ObservableCollection<ClientWithOrder> _clients;
        public ObservableCollection<ClientWithOrder> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged(nameof(Clients));
            }
        }

        private ObservableCollection<ClientWithOrder> _filteredClients;
        public ObservableCollection<ClientWithOrder> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                ClientsView = CollectionViewSource.GetDefaultView(FilteredClients);
                ClientsView.Filter = FilterClients;
                OnPropertyChanged(nameof(FilteredClients));
            }
        }

        public ICollectionView ClientsView { get; private set; }

        private ClientWithOrder _selectedClient;
        public ClientWithOrder SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                ClientsView?.Refresh();
                OnPropertyChanged(nameof(SearchText));
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
                    ClientsView?.Refresh();
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
                    ClientsView?.Refresh();
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

        public string DialogTitle => IsEditMode ? "Редактирование клиента" : "Добавление нового клиента";

        private Client _editingClient;
        public Client EditingClient
        {
            get => _editingClient;
            set
            {
                _editingClient = value;
                OnPropertyChanged(nameof(EditingClient));
            }
        }

        #endregion

        #region Commands

        public RelayCommand LoadClientsCommand { get; private set; }
        public RelayCommand AddClientCommand { get; private set; }
        public RelayCommand EditClientCommand { get; private set; }
        public RelayCommand DeleteClientCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand ExportClientsCommand { get; private set; }
        public RelayCommand SaveClientCommand { get; private set; }
        public RelayCommand CancelEditCommand { get; private set; }
        public RelayCommand SetTodayFilterCommand { get; private set; }
        public RelayCommand SetWeekFilterCommand { get; private set; }
        public RelayCommand SetMonthFilterCommand { get; private set; }
        public RelayCommand StartThumbDragDeltaCommand { get; private set; }
        public RelayCommand EndThumbDragDeltaCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadClientsCommand = new RelayCommand(ExecuteLoadClients, CanExecuteLoadClients);
            AddClientCommand = new RelayCommand(ExecuteAddClient);
            EditClientCommand = new RelayCommand(ExecuteEditClient, CanExecuteEditDelete);
            DeleteClientCommand = new RelayCommand(ExecuteDeleteClient, CanExecuteEditDelete);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            ExportClientsCommand = new RelayCommand(ExecuteExportClients);
            SaveClientCommand = new RelayCommand(ExecuteSaveClient);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
            SetTodayFilterCommand = new RelayCommand(ExecuteSetTodayFilter);
            SetWeekFilterCommand = new RelayCommand(ExecuteSetWeekFilter);
            SetMonthFilterCommand = new RelayCommand(ExecuteSetMonthFilter);
            StartThumbDragDeltaCommand = new RelayCommand(ExecuteStartThumbDragDelta);
            EndThumbDragDeltaCommand = new RelayCommand(ExecuteEndThumbDragDelta);
        }

        private bool CanExecuteLoadClients(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedClient != null;

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

        private void ExecuteLoadClients(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка клиентов...";

                var clients = _clientsRepository.GetAll()
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();

                Clients.Clear();

                // Добавляем клиентов с порядковыми номерами
                int orderNumber = 1;
                foreach (var client in clients)
                {
                    Clients.Add(new ClientWithOrder
                    {
                        Client = client,
                        OrderNumber = orderNumber++
                    });
                }
                UpdateFilteredClients();

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

        private void ExecuteAddClient(object parameter)
        {
            try
            {
                IsEditMode = false;
                EditingClient = new Client
                {
                    CreatedDate = DateTime.Now
                };
                IsDialogOpen = true;
                StatusMessage = "Добавление нового клиента";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления клиента: {ex.Message}";
                MessageBox.Show($"Ошибка добавления клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditClient(object parameter)
        {
            if (SelectedClient == null) return;

            try
            {
                IsEditMode = true;
                // Создаем копию клиента для редактирования
                EditingClient = new Client
                {
                    Id = SelectedClient.Client.Id,
                    CompanyName = SelectedClient.Client.CompanyName,
                    ContactPerson = SelectedClient.Client.ContactPerson,
                    Email = SelectedClient.Client.Email,
                    Phone = SelectedClient.Client.Phone,
                    Address = SelectedClient.Client.Address,
                    CreatedDate = SelectedClient.Client.CreatedDate
                };
                IsDialogOpen = true;
                StatusMessage = $"Редактирование клиента: {SelectedClient.Client.CompanyName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка редактирования клиента: {ex.Message}";
                MessageBox.Show($"Ошибка редактирования клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteClient(object parameter)
        {
            if (SelectedClient == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента \"{SelectedClient.Client.CompanyName}\"?\n\n" +
                "Все связанные задачи также будут удалены!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _clientsRepository.Delete(SelectedClient.Client.Id);
                    _clientsRepository.Save();
                    ExecuteLoadClients(null);
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

        private void ExecuteSaveClient(object parameter)
        {
            try
            {
                if (EditingClient == null) return;

                // Валидация
                if (string.IsNullOrWhiteSpace(EditingClient.CompanyName))
                {
                    MessageBox.Show("Название компании обязательно для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EditingClient.Email))
                {
                    MessageBox.Show("Email обязателен для заполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsEditMode)
                {
                    // Обновление существующего клиента
                    var existingClient = _clientsRepository.GetById(EditingClient.Id);
                    if (existingClient != null)
                    {
                        existingClient.CompanyName = EditingClient.CompanyName;
                        existingClient.ContactPerson = EditingClient.ContactPerson;
                        existingClient.Email = EditingClient.Email;
                        existingClient.Phone = EditingClient.Phone;
                        existingClient.Address = EditingClient.Address;

                        _clientsRepository.Update(existingClient);
                        StatusMessage = $"Клиент \"{existingClient.CompanyName}\" обновлен";
                    }
                }
                else
                {
                    // Создание нового клиента
                    EditingClient.CreatedDate = DateTime.Now;
                    _clientsRepository.Create(EditingClient);
                    StatusMessage = $"Новый клиент \"{EditingClient.CompanyName}\" создан";
                }

                _clientsRepository.Save();
                IsDialogOpen = false;
                ExecuteLoadClients(null);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения клиента: {ex.Message}";
                MessageBox.Show($"Ошибка сохранения клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancelEdit(object parameter)
        {
            IsDialogOpen = false;
            EditingClient = null;
            StatusMessage = "Редактирование отменено";
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            FilterStartDate = DateTime.Today.AddDays(-30);
            FilterEndDate = DateTime.Today.AddDays(30);
            StatusMessage = "Фильтры очищены";
        }

        private void ExecuteExportClients(object parameter)
        {
            try
            {
                var exportCount = FilteredClients.Count;

                StatusMessage = $"Экспортировано {exportCount} клиентов";
                MessageBox.Show($"Готово к экспорту {exportCount} клиентов", "Экспорт",
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
            ExecuteLoadClients(null);
        }

        private void UpdateFilteredClients()
        {
            FilteredClients.Clear();

            // Обновляем порядковые номера для отфильтрованных клиентов
            int orderNumber = 1;
            foreach (var clientWithOrder in Clients.Where(c => FilterClients(c)))
            {
                clientWithOrder.OrderNumber = orderNumber++;
                FilteredClients.Add(clientWithOrder);
            }
            ClientsView?.Refresh();
        }

        private bool FilterClients(object obj)
        {
            if (obj is not ClientWithOrder clientWithOrder) return false;
            var client = clientWithOrder.Client;

            // Фильтр по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = client.CompanyName?.ToLower().Contains(searchLower) == true ||
                             client.ContactPerson?.ToLower().Contains(searchLower) == true ||
                             client.Email?.ToLower().Contains(searchLower) == true ||
                             client.Phone?.ToLower().Contains(searchLower) == true ||
                             client.Address?.ToLower().Contains(searchLower) == true;

                if (!matches) return false;
            }

            // Фильтр по диапазону дат
            if (FilterStartDate.HasValue && client.CreatedDate.Date < FilterStartDate.Value.Date)
                return false;

            if (FilterEndDate.HasValue && client.CreatedDate.Date > FilterEndDate.Value.Date)
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