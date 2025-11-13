using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;
using Task = WishList.Model.Entity.Task;

namespace WishList
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TaskProgress> TaskProgresses { get; set; }
        public DbSet<WorkPlan> WorkPlans { get; set; }


        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<TaskStatuss> TaskStatuses { get; set; }
        public DbSet<TaskPriority> TaskPriorities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=SupportManager;Trusted_Connection=True;";

                optionsBuilder.UseSqlServer(
             connectionString,
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
