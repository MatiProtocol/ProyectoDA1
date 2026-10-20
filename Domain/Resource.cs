namespace Domain;

public class Resource
{
    private int _id;
    private string _name;
    private string _type;
    private string _description;
    private bool _isExclusive;
    private int _quantity;
    
    private List<(ProjectTask task, int quantity, int priority)> _queue = new List<(ProjectTask, int, int)>();
    private DateTime _startUseDate;
    private DateTime _endUseDate;

    public Resource(string name, string type, string description, int quantity)
    {
        if (!string.IsNullOrEmpty(name)) _name = name;
        else throw new ArgumentException("El nombre del recurso no puede ser vacio.");
        _type = type;
        _description = description;
        _isExclusive = false;
        _quantity = quantity;
    }

    public int Quantity
    {
        get => _quantity;
        set => _quantity = value;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("El nombre del recurso no puede ser vacio.");
            _name = value;
        }
    }

    public List<(ProjectTask task, int quantity, int priority)> Queue
    {
        get => _queue;
        set => _queue = value;
    }

    public string Type
    {
        get => _type;
        set => _type = value;

    }

    public string Description
    {
        get => _description;
        set => _description = value;
    }

    public bool Exclusive
    {
        get => _isExclusive;
        set => _isExclusive = value;
    }

    public void ToggleExclusiveness() => _isExclusive = !_isExclusive;

    public int Id
    {
        get => _id;
        set => _id = value;
    }
}