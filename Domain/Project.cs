using System.Diagnostics;
using Auxiliaries;

namespace Domain;

public class Project
{

    private int _id;
    
    private string _name;
    private string _description;
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Now;
    private DateTime _delay = DateTime.Now;
    private List<ProjectTask> _tasks;
    private ProjectTeam _team;
    private TaskGraph _graph;
    private Dictionary<string, List<(Resource, int)>> _resourceRelation { set; get; }
    private List<(Resource, int)> _resourceExclusive { set; get; }
    private Dictionary<string, List<User>> _taskToUsers = new Dictionary<string, List<User>>();
    private Dictionary<string, List<ProjectTask>> _userToTask = new Dictionary<string, List<ProjectTask>>();

    public Project() {}
    
    public Project(string name, string desc)
    {
        _name = name;
        _tasks = [];
        _team = new ProjectTeam();
        if (ValidateDesc(desc) == null) _description = desc;
        else throw new ArgumentException("The description must not longer than 400 characters.");
        _graph = new TaskGraph();
        _resourceRelation = new Dictionary<string, List<(Resource, int)>>();
        _resourceExclusive = [];
    }

    public int Id
    {
        get => _id;
        set => _id = value;
    }
    
    public ProjectTeam Team
    {
        get => _team;
        set => _team = value;
    }

    public List<ProjectTask> TaskList
    {
        get => _tasks;
        set => _tasks = value;
    }

    public void AddTaskToList(ProjectTask task) => _tasks.Add(task);

    //PRE: The string title has to be related to a task that must exist and belong to the List<ProjectTask> Tasks 
    public ProjectTask GetTask(string title)
    {
        Debug.Assert(_tasks.Exists(x => x.Title.Equals(title)));

        return _tasks.Find(x => x.Title.Equals(title));
    }

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    public string Description
    {
        get => _description;
        set
        {
            var error = ValidateDesc(value);
            if (error != null) throw new ArgumentException(error.GetMessage());
            _description = value;
        }
    }

    private static Error? ValidateDesc(string desc) =>
        desc.Length <= 400 ? null : new Error("FormatError", "The description must not longer than 400 characters.");


    public DateTime StartDate
    {
        get => _startDate;
        set => _startDate = value;
    }

    public DateTime Delay
    {
        get => _delay;
        set => _delay = value;
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => _endDate = value;
    }

    public TaskGraph TaskGraph
    {
        get => _graph;
        set => _graph = value;
    }

    public bool ValidateProjectManager(string mail)
    {
        return _team.Manager != null && _team.Manager.Email == mail;
    }

    public Dictionary<string, List<(Resource, int)>> ResourceRelation
    {
        get => _resourceRelation;
        set => _resourceRelation = value;
    }

    public List<(Resource, int)> ResourceExclusive
    {
        get => _resourceExclusive;
        set => _resourceExclusive = value;
    }

    public void AddResourceRelation(string taskName, Resource resource, int quantity)
    {
        Debug.Assert(_tasks.Exists(x => x.Title.Equals(taskName)));
        if (quantity > resource.Quantity) return;

        var task = TaskList.Find(task => task.Title == taskName);

        resource.Queue.Add((task!, quantity, 0));

        if (_resourceRelation.ContainsKey(taskName))
        {
            if (_resourceRelation[taskName].Exists(r => r.Item1.Name.Equals(resource.Name) && r.Item1.Type.Equals(resource.Type)))
            {
                var valueTuple = _resourceRelation[taskName].Find(r => r.Item1.Name.Equals(resource.Name));
                valueTuple.Item2 += quantity;
            }
            else _resourceRelation[taskName].Add((resource, quantity));
        }
        else
        {
            _resourceRelation.Add(taskName, new List<(Resource, int)>());
            _resourceRelation[taskName].Add((resource, quantity));
        }
    }

    public void AddResourceExclusive(Resource resource, int quantity)
    {
        if (quantity > resource.Quantity) return;
        if (_resourceExclusive.Exists(r => r.Item1.Name.Equals(resource.Name)))
        {
            var valueTuple = _resourceExclusive.Find(r => r.Item1.Name.Equals(resource.Name));
            valueTuple.Item2 += quantity;
            return;
        }

        if (!resource.Exclusive) resource.ToggleExclusiveness();
        _resourceExclusive.Add((resource, quantity));
    }

    public void RemoveResourceExclusive(Resource resource)
    {
        if (!_resourceExclusive.Exists(r => r.Item1.Name.Equals(resource.Name))) return;
        if (resource.Exclusive) resource.ToggleExclusiveness();

        int quantity = _resourceExclusive.Find(r => r.Item1.Name.Equals(resource.Name)).Item2;
        _resourceExclusive.Remove((resource, quantity));
    }

    public void AssignedUserToTask(ProjectTask task, User user)
    {
        Debug.Assert(
            _tasks.Find(t => t.Title == task.Title) != null &&
            (
                _team.GetAMember(user) != null ||
                (
                    _team.Manager != null &&
                    _team.Manager!.Email == user.Email)
            )
        );
        var taskName = task.Title;

        if (!_taskToUsers.ContainsKey(taskName))
        {
            _taskToUsers.Add(taskName, new List<User>());
            if (!_userToTask.ContainsKey(user.Email)) _userToTask[user.Email] = new List<ProjectTask>();
        }

        if (_taskToUsers[taskName].Exists(u => u.Email.Equals(user.Email))) return;
        _taskToUsers[taskName].Add(user);
        _userToTask[user.Email].Add(task);
    }

    public User[] GetUserAssignedToTask(ProjectTask task)
        => _taskToUsers.TryGetValue(task.Title, out var value) ? value.ToArray() : [];

    public ProjectTask[] GetTaskAssignedToUser(User user)
    {
        return _userToTask[user.Email].ToArray();
    }

    public bool UnassignUserToTask(ProjectTask task, User user)
    {
        Debug.Assert(
            _tasks.Find(t => t.Title == task.Title) != null &&
            (
                _team.GetAMember(user) != null ||
                (
                    _team.Manager != null &&
                    _team.Manager!.Email == user.Email)
            )
        );
        var taskName = task.Title;
        var userMail = user.Email;

        if (!_userToTask.ContainsKey(userMail)) return false;
        if (!_taskToUsers.ContainsKey(taskName)) return false;

        var listTask = _userToTask[userMail];
        var listUser = _taskToUsers[taskName];

        var indexUser = listUser.FindIndex(u => u.Email.Equals(userMail));
        if (indexUser == -1) return false;
        listUser.RemoveAt(indexUser);

        var indexTask = listTask.FindIndex(t => t.Title.Equals(taskName));
        listTask.RemoveAt(indexTask);

        return true;

    }

    public Dictionary<string, List<User>> TaskToUser
    {
        get => _taskToUsers;
        set => _taskToUsers = value;
    }

    public Dictionary<string, List<ProjectTask>> UserToTask
    {
        get => _userToTask;
        set => _userToTask = value;
    }
}

