// Models/Entity/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int UserId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("birth_date")]
        public DateOnly Birth_date { get; set; }

        [Column("reset_token")]
        public string Reset_token { get; set; } = string.Empty;

        [Column("reset_expires")]
        public DateTime Reset_expires { get; set; }

    }
}