using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WishList.Model.Repository;
using WishList.ViewModel;
using WishList.ViewModel.AdminViewModel.Dop;

namespace WishList.ViewModel.AdminViewModel
{
    public class MainAdminViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationContext _context;

        public MainAdminViewModel()
        {
            _context = new ApplicationContext();

            // Создаем дочерние ViewModel
            TasksViewModel = new TasksViewModel();
            EmployeesViewModel = new EmployeesViewModel();
            ClientsViewModel = new ClientsViewModel();

            InitializeCommands();
            LoadStatistics(null);
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

        private void InitializeCommands()
        {
            LoadDataCommand = new RelayCommand(LoadStatistics);
            SwitchTabCommand = new RelayCommand(SwitchTab);
        }

        #endregion

        #region Methods

        private void LoadStatistics(object obj)
        {
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
                var completedTasks = tasksRepo.GetAll().Count(t => t.StatusId == 4); // Предполагаем, что 4 - ID завершенного статуса

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

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}