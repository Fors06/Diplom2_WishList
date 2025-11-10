using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;

namespace WishList
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=forus.tw1.ru;Port=3306;Database=cl76980_wishlist;Uid=cl76980_wishlist;Pwd=12345678900987654321;CharSet=utf8mb4;SslMode=None;";

                optionsBuilder.UseMySql(
             connectionString,
             new MySqlServerVersion(new Version(8, 0, 43)),
             options => options.EnableRetryOnFailure(
                 maxRetryCount: 5,
                 maxRetryDelay: TimeSpan.FromSeconds(30),
                 errorNumbersToAdd: null
             )
         );
            }

        }
    }
}
