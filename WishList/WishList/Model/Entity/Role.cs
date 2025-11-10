// Models/Entity/Role.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishList.Model.Entity
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [Column("RoleName")]
        public string RoleName { get; set; } = string.Empty;
    }
    //public enum UserTypes
    //{
    //    Admin = 1,
    //    Manager = 2,
    //    Developer = 3,
    //    Designer = 4
    //}
}