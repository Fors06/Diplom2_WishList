using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using WishList.Model.Repository.Abstraction;
using WishList.Model.Entity;
using Task = WishList.Model.Entity.Task;

namespace WishList.Model.Repository
{
    public class TasksRepository : IRepository<Task>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public TasksRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<Task> GetAll()
        {
            return _context.Tasks
                .Include(t => t.Client)
                .Include(t => t.Category)
                .Include(t => t.Manager)
                .Include(t => t.Programmer)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .AsQueryable();
        }

        public IQueryable<Task> Find(Expression<Func<Task, bool>> predicate)
        {
            return _context.Tasks
                .Include(t => t.Client)
                .Include(t => t.Category)
                .Include(t => t.Manager)
                .Include(t => t.Programmer)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Where(predicate);
        }

        public Task GetById(int id)
        {
            return _context.Tasks
                .Include(t => t.Client)
                .Include(t => t.Category)
                .Include(t => t.Manager)
                .Include(t => t.Programmer)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.TaskProgress)
                .FirstOrDefault(t => t.Id == id);
        }

        public void Create(Task task)
        {
            _context.Tasks.Add(task);
        }

        public void Update(Task task)
        {
            _context.Entry(task).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            Task task = _context.Tasks.Find(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
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