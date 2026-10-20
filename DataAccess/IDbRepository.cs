using System.Linq.Expressions;
using Auxiliaries;

namespace DataAccess;

public interface IDbRepository<T, in TKey>
{
    Error? Add(T entity);
    T? GetById(TKey id);
    List<T> GetAll();
    Error? Update(T entity, bool reactive = true);
    bool Remove(TKey id);
    IQueryable<T> Query();
    T? FindBy(Expression<Func<T, bool>> predicate);
    bool Exists(Expression<Func<T, bool>> predicate);
}