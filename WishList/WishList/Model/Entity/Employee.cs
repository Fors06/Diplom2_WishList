
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("Сотрудники")]
    public class Employee
    {
        [Key]
        [Column("СотрудникId")]
        public int Id { get; set; }

        [Column("Должность")]
        public string Должность { get; set; } = String.Empty;

        [Column("ДатаПриема")]
        public DateTime ДатаПриема {  get; set; }

        [Column("ПользовательId")]
        public int ПользовательId { get; set; }

        [ForeignKey("ПользовательId")] 
        public virtual User Users { get; set; }
    }
}
