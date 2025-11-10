// Repository/UsersRepository.cs
using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;
using WishList.Model.Repository.Abstraction;
using System.Linq.Expressions;

namespace WishList.Model.Repository
{
    public class UsersRepository : IRepository<User>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public UsersRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<User> GetAll()
        {
            return _context.Users.AsQueryable();
        }

        public IQueryable<User> Find(Expression<Func<User, bool>> predicate)
        {
            return _context.Users.Where(predicate);
        }

        public User GetById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == id);
        }

        public void Create(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            User user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
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