using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("Tasks")]
    public class Task
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Title")]
        public string Title { get; set; } = string.Empty;

        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Column("ClientId")]
        public int ClientId { get; set; }

        [Column("CategoryId")]
        public int CategoryId { get; set; }

        [Column("ManagerId")]
        public int ManagerId { get; set; }

        [Column("ProgrammerId")]
        public int? ProgrammerId { get; set; }

        [Column("StatusId")]
        public int StatusId { get; set; }

        [Column("PriorityId")]
        public int PriorityId { get; set; }

        [Column("TaskProgressId")]
        public int TaskProgressId { get; set; }

        [Column("WorkPlansId")]
        public int WorkPlansId { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("DueDate")]
        public DateTime? DueDate { get; set; }

        [Column("CompletedDate")]
        public DateTime? CompletedDate { get; set; }

        [Column("EstimatedHours")]
        public decimal? EstimatedHours { get; set; }

        [Column("ActualHours")]
        public decimal? ActualHours { get; set; }

        // Навигационные свойства
        public virtual Client Client { get; set; }
        public virtual TaskCategory Category { get; set; }
        public virtual Employee Manager { get; set; }
        public virtual Employee Programmer { get; set; }
        public virtual TaskStatus Status { get; set; }
        public virtual TaskPriority Priority { get; set; }
        public virtual TaskProgress TaskProgress { get; set; }
        public virtual WorkPlan WorkPlan { get; set; }
    }
}