using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WishList.Model.Entity
{
    [Table("WorkPlans")]
    public class WorkPlan
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("PlanDescription")]
        public string PlanDescription { get; set; } = string.Empty;

        [Column("TestSteps")]
        public string TestSteps { get; set; } = string.Empty;

        [Column("EstimatedHours")]
        public decimal EstimatedHours { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        //// Вычисляемые свойства
        //[NotMapped]
        //public string FormattedCreatedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        //[NotMapped]
        //public string ShortPlanDescription => PlanDescription.Length > 150
        //    ? PlanDescription.Substring(0, 150) + "..."
        //    : PlanDescription;
    }
}
