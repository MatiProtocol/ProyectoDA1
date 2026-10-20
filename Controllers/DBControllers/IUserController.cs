using System.Linq.Expressions;
using Auxiliaries;

namespace Controllers.DBControllers;

public interface IUserController<TEntity, TDto, in TKey>
{
    public Error? Add(TDto entity);
    
    public TDto? GetById(TKey id);
    
    public List<TDto> GetAll();
    
    public Error? Update(TDto entity);
    
    public bool Remove(TKey id);

    public TEntity? FindBy(Expression<Func<TEntity, bool>> predicate);
    
    public bool Exists(Expression<Func<TEntity, bool>> predicate);
}