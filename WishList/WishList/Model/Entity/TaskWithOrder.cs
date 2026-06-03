using System;
using System.ComponentModel;
using WishList.Model.Entity;

namespace WishList.Model.Entity
{
    public class TaskWithOrder : INotifyPropertyChanged
    {
        private Task _task;
        public Task Task
        {
            get => _task;
            set
            {
                _task = value;
                OnPropertyChanged(nameof(Task));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(Client));
                OnPropertyChanged(nameof(Category));
                OnPropertyChanged(nameof(Priority));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(DueDate));
                OnPropertyChanged(nameof(CreatedDate));
                OnPropertyChanged(nameof(Manager));
                OnPropertyChanged(nameof(Programmer));
                OnPropertyChanged(nameof(OrderNumber));
            }
        }

        public int OrderNumber { get; set; }

        public int Id => Task?.Id ?? 0;
        public string Title => Task?.Title ?? string.Empty;
        public string Description => Task?.Description ?? string.Empty;
        public Client Client => Task?.Client;
        public TaskCategory Category => Task?.Category;
        public TaskPriority Priority => Task?.Priority;
        public TaskStatuss Status => Task?.Status;
        public DateTime? DueDate => Task?.DueDate;
        public DateTime CreatedDate => Task?.CreatedDate ?? DateTime.MinValue;
        public Employee Manager => Task?.Manager;
        public Employee Programmer => Task?.Programmer;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}