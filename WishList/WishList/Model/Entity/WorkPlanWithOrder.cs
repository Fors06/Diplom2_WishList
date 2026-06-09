using System;
using System.ComponentModel;
using WishList.Model.Entity;

namespace WishList.Model.Entity
{
    public class WorkPlanWithOrder : INotifyPropertyChanged
    {
        private WorkPlan _workPlan;
        private bool _isCompleted;

        public WorkPlan WorkPlan
        {
            get => _workPlan;
            set
            {
                _workPlan = value;
                OnPropertyChanged(nameof(WorkPlan));
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(PlanDescription));
                OnPropertyChanged(nameof(TestSteps));
                OnPropertyChanged(nameof(EstimatedHours));
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public int OrderNumber { get; set; }
        public int Id => WorkPlan?.Id ?? 0;
        public string PlanDescription => WorkPlan?.PlanDescription ?? string.Empty;
        public string TestSteps => WorkPlan?.TestSteps ?? string.Empty;
        public decimal EstimatedHours => WorkPlan?.EstimatedHours ?? 0;
        public DateTime CreatedDate => WorkPlan?.CreatedDate ?? DateTime.MinValue;

        // Локальное свойство для пометки выполнения
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}