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
    public class TaskProgressRepository : IRepository<TaskProgress>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public TaskProgressRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<TaskProgress> GetAll()
        {
            return _context.TaskProgresses.AsQueryable();
        }

        public IQueryable<TaskProgress> Find(Expression<Func<TaskProgress, bool>> predicate)
        {
            return _context.TaskProgresses.Where(predicate);
        }

        public TaskProgress GetById(int id)
        {
            return _context.TaskProgresses.Find(id);
        }

        public void Create(TaskProgress role)
        {
            _context.TaskProgresses.Add(role);
        }

        public void Update(TaskProgress role)
        {
            _context.Entry(role).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            TaskProgress role = _context.TaskProgresses.Find(id);
            if (role != null)
            {
                _context.TaskProgresses.Remove(role);
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