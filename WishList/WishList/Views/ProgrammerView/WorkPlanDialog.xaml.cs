using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WishList.Model.Entity;
using WishList.ViewModel.ProgrammerViewModel;

namespace WishList.Views.ProgrammerView
{
    /// <summary>
    /// Логика взаимодействия для WorkPlanDialog.xaml
    /// </summary>
    public partial class WorkPlanDialog : Window
    {
        public WorkPlanDialog(WorkPlan workPlan, bool isEditMode)
        {
            InitializeComponent();
            DataContext = new WorkPlanDialogViewModel(workPlan, isEditMode, this);
        }
    }
}
