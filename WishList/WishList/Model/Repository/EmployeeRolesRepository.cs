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
    public class EmployeeRolesRepository : IRepository<EmployeeRole>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public EmployeeRolesRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeeRole> GetAll()
        {
            return _context.EmployeeRoles.AsQueryable();
        }

        public IQueryable<EmployeeRole> Find(Expression<Func<EmployeeRole, bool>> predicate)
        {
            return _context.EmployeeRoles.Where(predicate);
        }

        public EmployeeRole GetById(int id)
        {
            return _context.EmployeeRoles.Find(id);
        }

        public void Create(EmployeeRole role)
        {
            _context.EmployeeRoles.Add(role);
        }

        public void Update(EmployeeRole role)
        {
            _context.Entry(role).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            EmployeeRole role = _context.EmployeeRoles.Find(id);
            if (role != null)
            {
                _context.EmployeeRoles.Remove(role);
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