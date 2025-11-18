// Models/Entity/Client.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("Clients")]
    public class Client
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CompanyName")]
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [Column("ContactPerson")]
        [MaxLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Column("Email")]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Column("Phone")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column("Address")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ICollection<Task> Tasks { get; set; }

        public string Name => $"{CompanyName}";
    }
}