using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WishList.Model.Entity;
using WishList.Model.Repository.Abstraction;

namespace WishList.Model.Repository
{
    public class TaskWorkPlansRepository : IRepository<TaskWorkPlan>
    {
        private readonly ApplicationContext _context;
        private readonly DbSet<TaskWorkPlan> _dbSet;
        private bool _disposed = false;

        public TaskWorkPlansRepository(ApplicationContext context)
        {
            _context = context;
            _dbSet = context.Set<TaskWorkPlan>();
        }

        public IQueryable<TaskWorkPlan> GetAll()
        {
            return _dbSet
                .Include(twp => twp.Task)
                .Include(twp => twp.WorkPlan)
                .AsQueryable();
        }

        public IQueryable<TaskWorkPlan> Find(Expression<Func<TaskWorkPlan, bool>> predicate)
        {
            return GetAll().Where(predicate);
        }

        public TaskWorkPlan GetById(int id)
        {
            return GetAll().FirstOrDefault(twp => twp.Id == id);
        }

        public void Create(TaskWorkPlan item)
        {
            _dbSet.Add(item);
        }

        public void Update(TaskWorkPlan item)
        {
            _dbSet.Attach(item);
            _context.Entry(item).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            var item = GetById(id);
            if (item != null)
            {
                _dbSet.Remove(item);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}