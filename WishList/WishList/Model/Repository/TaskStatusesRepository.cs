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
    public class TaskStatusesRepository : IRepository<TaskStatuss>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public TaskStatusesRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<TaskStatuss> GetAll()
        {
            return _context.TaskStatuses.AsQueryable();
        }

        public IQueryable<TaskStatuss> Find(Expression<Func<TaskStatuss, bool>> predicate)
        {
            return _context.TaskStatuses.Where(predicate);
        }

        public TaskStatuss GetById(int id)
        {
            return _context.TaskStatuses.Find(id);
        }

        public void Create(TaskStatuss role)
        {
            _context.TaskStatuses.Add(role);
        }

        public void Update(TaskStatuss role)
        {
            _context.Entry(role).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            TaskStatuss role = _context.TaskStatuses.Find(id);
            if (role != null)
            {
                _context.TaskStatuses.Remove(role);
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