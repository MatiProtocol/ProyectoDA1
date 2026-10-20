using Auxiliaries;
namespace DataAccess;

public class Repository<TK, TV> where TK : notnull where TV : notnull
{
    private Dictionary<TK, TV> _dic = new Dictionary<TK, TV>();
    private Reactivity _reactivity = Reactivity.GetInstance();

    public Error? Add(TK k, TV v)
    {
        if (!_dic.TryAdd(k, v)) return new Error(Error.Types.KeyAlreadyExist, "La llave ya pertenece en el repositorio");
        _reactivity.Invoke();

        return null;
    }

    public (TV Value, bool Has) Get(TK k)
    {
        var exist = _dic.TryGetValue(k, out var temp);
        return (temp!, exist);
    }

    public (TK Key, TV Value)[] GetAll()
    {
        return _dic.Select(tuple => (tuple.Key, tuple.Value)).ToArray();
    }

    public bool Remove(TK k)
    {
        if (!_dic.Remove(k)) return false;
        _reactivity.Invoke();
        return true;
    }

    public Error? UpdateValue(TK k, TV newV)
    {
        if (!_dic.ContainsKey(k)) return new Error("KeyDoesNotExist", "Key does no belong to the dictionary");

        _dic.Remove(k);

        _dic.Add(k, newV);
        _reactivity.Invoke();

        return null;
    }

    public Error? UpdateKey(TK k, TK newK)
    {
        if (!_dic.ContainsKey(k)) return new Error("KeyDoesNotExist", "Key does no belong to the dictionay");
        if (_dic.ContainsKey(newK)) return new Error("KeyAlreadyExist", "Key does exist dictionary it, can not have a duplicate");
        var value = _dic[k];

        _dic.Remove(k);

        _dic.Add(newK, value);
        _reactivity.Invoke();

        return null;
    }

    public TV[] GetValues() => _dic.Values.ToArray();

    public void Reset()
    {
        _dic.Clear();
    }
}
