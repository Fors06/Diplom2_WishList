using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}
