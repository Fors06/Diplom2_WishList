using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WishList.Model.Entity;

namespace WishList.ViewModel.ProgrammerViewModel
{
    public class WorkPlanDialogViewModel : INotifyPropertyChanged
    {
        private readonly WorkPlan _originalWorkPlan;
        private readonly bool _isEditMode;
        private readonly Window _dialog;

        public WorkPlanDialogViewModel(WorkPlan workPlan, bool isEditMode, Window dialog)
        {
            _originalWorkPlan = workPlan;
            _isEditMode = isEditMode;
            _dialog = dialog;

            DialogTitle = isEditMode ? "✏️ Редактирование плана работ" : "➕ Создание плана работ";

            if (isEditMode)
            {
                // Режим редактирования - заполняем существующими данными
                EditingWorkPlan = new WorkPlan
                {
                    Id = workPlan.Id,
                    PlanDescription = workPlan.PlanDescription,
                    TestSteps = workPlan.TestSteps,
                    EstimatedHours = workPlan.EstimatedHours,
                    CreatedDate = workPlan.CreatedDate
                };
            }
            else
            {
                // Режим создания - пустые поля, подсказки будут из Tag
                EditingWorkPlan = new WorkPlan
                {
                    Id = 0,
                    PlanDescription = string.Empty,  // Пустая строка!
                    TestSteps = string.Empty,        // Пустая строка!
                    EstimatedHours = 0,
                    CreatedDate = DateTime.Now
                };
            }
        }

        public string DialogTitle { get; }
        public WorkPlan EditingWorkPlan { get; }

        public RelayCommand SaveCommand => new RelayCommand(_ => Save());
        public RelayCommand CancelCommand => new RelayCommand(_ => Cancel());

        private void Save()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(EditingWorkPlan.PlanDescription))
            {
                MessageBox.Show("Введите название плана", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingWorkPlan.TestSteps))
            {
                MessageBox.Show("Введите план действий", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingWorkPlan.EstimatedHours <= 0)
            {
                MessageBox.Show("Плановые часы должны быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем изменения
            _originalWorkPlan.PlanDescription = EditingWorkPlan.PlanDescription;
            _originalWorkPlan.TestSteps = EditingWorkPlan.TestSteps;
            _originalWorkPlan.EstimatedHours = EditingWorkPlan.EstimatedHours;

            _dialog.DialogResult = true;
            _dialog.Close();
        }

        private void Cancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
