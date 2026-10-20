using System.Linq.Expressions;
using Auxiliaries;
using DBContext;

namespace DataAccess;

public class RepositoryEf<T, TKey> : IDbRepository<T, TKey> where T : class
{
    private readonly ISqlContextFactory _contextFactory;
    private readonly Func<T, Expression<Func<T, bool>>> _uniqueRule;
    private readonly Func<T, TKey> _getKeyValue;
    private readonly Func<T, SqlContext, T> _construct;
    private readonly Reactivity _reactivity = Reactivity.GetInstance();

    public RepositoryEf(ISqlContextFactory contextFactory, Func<T, Expression<Func<T, bool>>> uniqueRule, Func<T, TKey> getKeyValue, Func<T, SqlContext, T>? construct = null)
    {
        _contextFactory = contextFactory;
        _uniqueRule = uniqueRule;
        _getKeyValue = getKeyValue;
        _construct = construct ?? ((x, _) => x);
    }

    public Error? Add(T entity)
    {
        if (Exists(_uniqueRule(entity))) return new Error("KeyAlreadyExist", "La llave ya pertenece al repositorio");

        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        var existingEntity = _find(context, entity);
        if (existingEntity != null)
            return new Error(Error.Types.KeyDoesNotExist, "La llave ya pertenece al repositorio");

        try
        {
            dbSet.Add(entity);
        }
        catch
        {
            return new Error(Error.Types.KeyAlreadyExist, "El elemento ya existe");
        }

        context.SaveChanges();
        _reactivity.Invoke();
        return null;
    }

    public T? GetById(TKey id)
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        if (id is ValueTuple<int, int> tupleKey) return dbSet.Find(tupleKey.Item1, tupleKey.Item2);
        var result = dbSet.Find(id);
        return result == null ? null : _construct(result, context);
    }

    public List<T> GetAll()
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        return dbSet.Select(x => _construct(x, context)).ToList();
    }

    public Error? Update(T entity, bool react = true)
    {
        if (Exists(_uniqueRule(entity)))
            return new Error(Error.Types.KeyAlreadyExist, "La llave ya pertenece al repositorio");

        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        var existingEntity = _find(context, entity);
        if (existingEntity == null)
            return new Error(Error.Types.KeyDoesNotExist, "La entidad no existe en el repositorio");

        context.Entry(existingEntity).CurrentValues.SetValues(entity);
        context.SaveChanges();
        if (react) _reactivity.Invoke();
        return null;
    }

    public bool Remove(TKey id)
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        var entity = GetById(id);
        if (entity == null) return false;
        dbSet.Remove(entity);
        context.SaveChanges();
        _reactivity.Invoke();
        return true;
    }

    public IQueryable<T> Query()
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();
        return dbSet.Select(x => _construct(x, context));
    }

    public IQueryable<T> Query(Func<SqlContext, IQueryable<T>> f)
    {
        var context = _contextFactory.Generate();

        return f(context).Select(t => _construct(t, context));
    }

    public T? FindBy(Expression<Func<T, bool>> predicate)
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();
        var result = dbSet.FirstOrDefault(predicate);
        return result == null ? null : _construct(result, context);
    }

    public bool Exists(Expression<Func<T, bool>> predicate)
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();
        return dbSet.Any(predicate);
    }

    public SqlContext GetContext()
    {
        var context = _contextFactory.Generate();
        context.Set<T>();

        return context;
    }

    public void Reset()
    {
        var context = _contextFactory.Generate();
        var dbSet = context.Set<T>();

        dbSet.RemoveRange(dbSet);
        context.SaveChanges();
        _reactivity.Invoke();
    }


    private T? _find(SqlContext context, T entity)
    {
        var dbSet = context.Set<T>();

        var key = _getKeyValue(entity);
        if (key is ValueTuple<int, int> tupleKey) return dbSet.Find(tupleKey.Item1, tupleKey.Item2);
        return dbSet.Find(key);
    }
}
