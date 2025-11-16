using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WishList.Model.Entity
{
    [Table("TaskProgress")]
    public class TaskProgress
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Column("ProgressPercentage")]
        public int ProgressPercentage { get; set; } = 0;

        [Column("HoursSpent")]
        public decimal? HoursSpent { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Вычисляемые свойства
        //[NotMapped]
        //public string FormattedCreatedDate => CreatedDate.ToString("dd.MM.yyyy HH:mm");

        //[NotMapped]
        //public string ShortDescription => Description.Length > 100
        //    ? Description.Substring(0, 100) + "..."
        //    : Description;
    }
}
