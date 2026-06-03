using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("TaskWorkPlans")]
    public class TaskWorkPlan
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        public int WorkPlanId { get; set; }
        [ForeignKey("WorkPlanId")]
        public virtual WorkPlan WorkPlan { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsPrimary { get; set; } = false;
    }
}