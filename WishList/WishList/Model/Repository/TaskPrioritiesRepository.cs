using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using WishList.Model.Entity;
using WishList.Model.Repository.Abstraction;

namespace WishList.Model.Repository
{
    public class TaskPrioritiesRepository : IRepository<TaskPriority>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public TaskPrioritiesRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<TaskPriority> GetAll()
        {
            return _context.TaskPriorities.AsQueryable();
        }

        public IQueryable<TaskPriority> Find(Expression<Func<TaskPriority, bool>> predicate)
        {
            return _context.TaskPriorities.Where(predicate);
        }

        public TaskPriority GetById(int id)
        {
            return _context.TaskPriorities.Find(id);
        }

        public void Create(TaskPriority role)
        {
            _context.TaskPriorities.Add(role);
        }

        public void Update(TaskPriority role)
        {
            _context.Entry(role).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            TaskPriority role = _context.TaskPriorities.Find(id);
            if (role != null)
            {
                _context.TaskPriorities.Remove(role);
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
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}