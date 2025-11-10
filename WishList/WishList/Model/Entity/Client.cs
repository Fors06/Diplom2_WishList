// Models/Entity/Client.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("Clients")]
    public class Client
    {
        [Key]
        [Column("ClientId")]
        public int ClientId { get; set; }

        [Column("CompanyName")]
        public string CompanyName { get; set; }

        [Column("ContactPerson")]
        public string ContactPerson { get; set; }

        [Column("Email")]
        public string Email { get; set; }

        [Column("Phone")]
        public string Phone { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }
    }
}