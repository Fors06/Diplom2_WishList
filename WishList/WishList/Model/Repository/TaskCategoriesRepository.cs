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
    public class TaskCategoriesRepository : IRepository<TaskCategory>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public TaskCategoriesRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<TaskCategory> GetAll()
        {
            return _context.TaskCategories.AsQueryable();
        }

        public IQueryable<TaskCategory> Find(Expression<Func<TaskCategory, bool>> predicate)
        {
            return _context.TaskCategories.Where(predicate);
        }

        public TaskCategory GetById(int id)
        {
            return _context.TaskCategories.Find(id);
        }

        public void Create(TaskCategory category)
        {
            _context.TaskCategories.Add(category);
        }

        public void Update(TaskCategory category)
        {
            _context.Entry(category).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            TaskCategory category = _context.TaskCategories.Find(id);
            if (category != null)
            {
                _context.TaskCategories.Remove(category);
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