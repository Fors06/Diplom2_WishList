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
    public class WorkPlansRepository : IRepository<WorkPlan>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public WorkPlansRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<WorkPlan> GetAll()
        {
            return _context.WorkPlans.AsQueryable();
        }

        public IQueryable<WorkPlan> Find(Expression<Func<WorkPlan, bool>> predicate)
        {
            return _context.WorkPlans.Where(predicate);
        }

        public WorkPlan GetById(int id)
        {
            return _context.WorkPlans.FirstOrDefault(wp => wp.Id == id);
        }

        public void Create(WorkPlan workPlan)
        {
            _context.WorkPlans.Add(workPlan);
        }

        public void Update(WorkPlan workPlan)
        {
            _context.Entry(workPlan).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            WorkPlan workPlan = _context.WorkPlans.Find(id);
            if (workPlan != null)
            {
                _context.WorkPlans.Remove(workPlan);
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