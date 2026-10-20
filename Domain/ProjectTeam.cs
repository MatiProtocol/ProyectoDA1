namespace Domain;

public class ProjectTeam
{
    private static int _nextId;
    private int _id;
    private Dictionary<string, User> _members;
    private User? _manager;
    private User? _leader;
    
    public ProjectTeam()
    {
        _members = new Dictionary<string, User>();
        _id = _nextId++;
    }
    
    public int Id
    {
        get => _id;
        set => _id = value;
    }
    
    public Dictionary<string, User> Members
    {
        get => _members;
        set => _members = value;
    }
    
    public User? Manager
    {
        get => _manager;
        set => _manager = value;
    }
    
    public User? Leader
    {
        get => _leader;
        set => _leader = value;
    }

    public void AddMember(User user) => _members.Add(user.Email, user);

    public User? GetAMember(User user) => _members.GetValueOrDefault(user.Email);

}