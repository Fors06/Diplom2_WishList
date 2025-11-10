// Repository/ClientsRepository.cs
using Microsoft.EntityFrameworkCore;
using WishList.Model.Entity;
using WishList.Model.Repository.Abstraction;
using System.Linq.Expressions;

namespace WishList.Model.Repository
{
    public class ClientsRepository : IRepository<Client>
    {
        private readonly ApplicationContext _context;
        private bool _disposed = false;

        public ClientsRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IQueryable<Client> GetAll()
        {
            return _context.Clients.AsQueryable();
        }

        public IQueryable<Client> Find(Expression<Func<Client, bool>> predicate)
        {
            return _context.Clients.Where(predicate);
        }

        public Client GetById(int id)
        {
            return _context.Clients.Find(id);
        }

        public void Create(Client client)
        {
            _context.Clients.Add(client);
        }

        public void Update(Client client)
        {
            _context.Entry(client).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            Client client = _context.Clients.Find(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
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