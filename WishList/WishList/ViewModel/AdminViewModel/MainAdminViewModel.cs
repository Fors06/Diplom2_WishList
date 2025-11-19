using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WishList.Model.Repository;
using WishList.ViewModel;
using WishList.ViewModel.AdminViewModel.Dop;
using WishList.Data.SwitchTheme;

namespace WishList.ViewModel.AdminViewModel
{
    public class MainAdminViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly DispatcherTimer _refreshTimer;
        private bool _disposed = false;

        public MainAdminViewModel()
        {
            _context = new ApplicationContext();

            // Создаем дочерние ViewModel
            TasksViewModel = new TasksViewModel();
            EmployeesViewModel = new EmployeesViewModel();
            ClientsViewModel = new ClientsViewModel();

            InitializeCommands();
            LoadStatistics(null);

            // Настраиваем таймер для автоматического обновления
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromMinutes(5);
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();
        }

        #region Child ViewModels

        public TasksViewModel TasksViewModel { get; }
        public EmployeesViewModel EmployeesViewModel { get; }
        public ClientsViewModel ClientsViewModel { get; }

        #endregion

        #region Properties

        private ObservableCollection<StatisticsCard> _statisticsCards;
        public ObservableCollection<StatisticsCard> StatisticsCards
        {
            get => _statisticsCards;
            set
            {
                _statisticsCards = value;
                OnPropertyChanged(nameof(StatisticsCards));
            }
        }

        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged(nameof(CurrentDate));
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

        public ICommand LoadDataCommand { get; private set; }
        public ICommand SwitchTabCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ToggleThemeCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadDataCommand = new RelayCommand(LoadStatistics);
            SwitchTabCommand = new RelayCommand(SwitchTab);
            RefreshCommand = new RelayCommand(_ => RefreshData());
            ToggleThemeCommand = new RelayCommand(ExecuteToggleTheme);
        }

        #endregion

        #region Methods

        private void RefreshData()
        {
            if (_disposed) return;

            try
            {
                LoadStatistics(null);
                CurrentDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                StatusMessage = $"Данные автоматически обновлены • {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при автоматическом обновлении: {ex.Message}";
            }
        }

        private void LoadStatistics(object obj)
        {
            if (_disposed) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка статистики...";

                CurrentDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                var tasksRepo = new TasksRepository(_context);
                var employeesRepo = new EmployeesRepository(_context);
                var clientsRepo = new ClientsRepository(_context);

                var totalTasks = tasksRepo.GetAll().Count();
                var activeEmployees = employeesRepo.GetAll().Count(e => e.IsActive);
                var totalClients = clientsRepo.GetAll().Count();
                var completedTasks = tasksRepo.GetAll().Count(t => t.StatusId == 4);

                StatisticsCards = new ObservableCollection<StatisticsCard>
                {
                    new StatisticsCard {
                        Title = "Всего заказов",
                        Value = totalTasks.ToString(),
                        Icon = "📋",
                        Color = "#3498db",
                        Description = "Все задачи в системе"
                    },
                    new StatisticsCard {
                        Title = "Активные сотрудники",
                        Value = activeEmployees.ToString(),
                        Icon = "👥",
                        Color = "#2ecc71",
                        Description = "Работающие сотрудники"
                    },
                    new StatisticsCard {
                        Title = "Клиенты",
                        Value = totalClients.ToString(),
                        Icon = "🏢",
                        Color = "#9b59b6",
                        Description = "Зарегистрированные клиенты"
                    },
                    new StatisticsCard {
                        Title = "Выполнено заказов",
                        Value = completedTasks.ToString(),
                        Icon = "✅",
                        Color = "#f1c40f",
                        Description = "Завершенные задачи"
                    }
                };

                StatusMessage = $"Данные успешно загружены • {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SwitchTab(object obj)
        {
            if (obj is int tabIndex)
            {
                SelectedTabIndex = tabIndex;
            }
        }

        private void ExecuteToggleTheme(object parameter)
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _refreshTimer?.Stop();
                _context?.Dispose();
                _disposed = true;
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}