// Repository/Abstraction/IRepository.cs
using System.Linq.Expressions;

namespace WishList.Model.Repository.Abstraction
{
    public interface IRepository<T> : IDisposable where T : class
    {
        IQueryable<T> GetAll();
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);
        T GetById(int id);
        void Create(T item);
        void Update(T item);
        void Delete(int id);
        void Save();
    }
}