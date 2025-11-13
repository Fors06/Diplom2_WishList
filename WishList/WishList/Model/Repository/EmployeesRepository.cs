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
    public class EmployeesRepository : IRepository<Employee>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public EmployeesRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<Employee> GetAll()
        {
            return _context.Employees.AsQueryable();
        }

        public IQueryable<Employee> Find(Expression<Func<Employee, bool>> predicate)
        {
            return _context.Employees.Where(predicate);
        }

        public Employee GetById(int id)
        {
            return _context.Employees.Find(id);
        }

        public void Create(Employee employee)
        {
            _context.Employees.Add(employee);
        }

        public void Update(Employee employee)
        {
            _context.Entry(employee).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            Employee employee = _context.Employees.Find(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
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
