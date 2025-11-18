using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WishList.Model.Entity;
using WishList.Model.Repository;
using WishList.ViewModel;

namespace WishList.ViewModel.AdminViewModel.Dop
{
    public class ClientsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;
        private readonly ClientsRepository _clientsRepository;

        public ClientsViewModel()
        {
            _context = new ApplicationContext();
            _clientsRepository = new ClientsRepository(_context);

            Clients = new ObservableCollection<Client>();
            FilteredClients = new ObservableCollection<Client>();

            ClientsView = CollectionViewSource.GetDefaultView(FilteredClients);
            ClientsView.Filter = FilterClients;

            InitializeCommands();
            LoadInitialData();
        }

        #region Properties

        private ObservableCollection<Client> _clients;
        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged(nameof(Clients));
            }
        }

        private ObservableCollection<Client> _filteredClients;
        public ObservableCollection<Client> FilteredClients
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

        private Client _selectedClient;
        public Client SelectedClient
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

        private DateTime? _filterCreatedDate;
        public DateTime? FilterCreatedDate
        {
            get => _filterCreatedDate;
            set
            {
                _filterCreatedDate = value;
                ClientsView?.Refresh();
                OnPropertyChanged(nameof(FilterCreatedDate));
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

        #endregion

        #region Commands

        public RelayCommand LoadClientsCommand { get; private set; }
        public RelayCommand AddClientCommand { get; private set; }
        public RelayCommand EditClientCommand { get; private set; }
        public RelayCommand DeleteClientCommand { get; private set; }
        public RelayCommand ClearFiltersCommand { get; private set; }
        public RelayCommand ExportClientsCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadClientsCommand = new RelayCommand(ExecuteLoadClients, CanExecuteLoadClients);
            AddClientCommand = new RelayCommand(ExecuteAddClient);
            EditClientCommand = new RelayCommand(ExecuteEditClient, CanExecuteEditDelete);
            DeleteClientCommand = new RelayCommand(ExecuteDeleteClient, CanExecuteEditDelete);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            ExportClientsCommand = new RelayCommand(ExecuteExportClients);
        }

        private bool CanExecuteLoadClients(object parameter) => !IsLoading;
        private bool CanExecuteEditDelete(object parameter) => SelectedClient != null;

        #endregion

        #region Command Methods

        private void ExecuteLoadClients(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка клиентов...";

                // Синхронная загрузка вместо асинхронной
                var clients = _clientsRepository.GetAll()
                    .OrderByDescending(c => c.CreatedDate)
                    .ToList();

                Clients.Clear();
                foreach (var client in clients)
                {
                    Clients.Add(client);
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
                var newClient = new Client
                {
                    CreatedDate = DateTime.Now
                };

                StatusMessage = "Добавление нового клиента - функция в разработке";
                MessageBox.Show("Функция добавления клиента находится в разработке", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                StatusMessage = $"Редактирование клиента: {SelectedClient.CompanyName}";
                MessageBox.Show($"Редактирование клиента: {SelectedClient.CompanyName}", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                $"Вы уверены, что хотите удалить клиента \"{SelectedClient.CompanyName}\"?\n\n" +
                "Все связанные задачи также будут удалены!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _clientsRepository.Delete(SelectedClient.Id);
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

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            FilterCreatedDate = null;
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

        #endregion

        #region Helper Methods

        private void LoadInitialData()
        {
            ExecuteLoadClients(null);
        }

        private void UpdateFilteredClients()
        {
            FilteredClients.Clear();
            foreach (var client in Clients)
            {
                FilteredClients.Add(client);
            }
            ClientsView?.Refresh();
        }

        private bool FilterClients(object obj)
        {
            if (obj is not Client client) return false;

            // Фильтр по поисковому тексту
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = client.CompanyName?.ToLower().Contains(searchLower) == true ||
                             client.Name?.ToLower().Contains(searchLower) == true ||
                             client.Email?.ToLower().Contains(searchLower) == true ||
                             client.Phone?.ToLower().Contains(searchLower) == true ||
                             client.Address?.ToLower().Contains(searchLower) == true;

                if (!matches) return false;
            }

            // Фильтр по дате создания
            if (FilterCreatedDate.HasValue && client.CreatedDate.Date != FilterCreatedDate.Value.Date)
                return false;

            return true;
        }

        #endregion

        #region Statistics

        public int TotalClients => Clients.Count;
        public int ClientsWithTasks => Clients.Count(c => c.Tasks?.Any() == true);
        public int ActiveClients => Clients.Count;

        public string GetClientsStatistics()
        {
            return $"Всего: {TotalClients} | С задачами: {ClientsWithTasks} | Активные: {ActiveClients}";
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}