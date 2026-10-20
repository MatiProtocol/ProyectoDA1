using System.Linq.Expressions;
using DBContext;
using Domain.HelpersDB;
using DomainConstruct;

namespace DataAccess;
using Domain;
public class PureDatabase
{
    public ISqlContextFactory _contextFactory;
    public RepositoryEf<User, int> Users { get; }
    public RepositoryEf<Project, int> Projects { get; }
    public RepositoryEf<Resource, int> Resources { get; }
    public RepositoryEf<ProjectTask, int> Tasks { get; }
    public RepositoryEf<TaskToTask, (int, int)> TaskToTasks { get; }
    public RepositoryEf<TaskResource, (int, int)> TaskResources { get; }
    public RepositoryEf<ProjectUser, (int, int)> ProjectUsers { get; }
    public RepositoryEf<TaskUser, (int, int)> TaskUsers { get; }

    public RepositoryEf<ProjectResourceExclusive, (int, int)> ProjectResourceExclusives { get; }

    public RepositoryEf<Notification,int> Notifications { get; }

    private static PureDatabase? _instance;

    public static PureDatabase GetInstance(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        if (_instance == null) _instance = new PureDatabase(contextFactory);
        return _instance;
    }

    public static PureDatabase GetNewInstance(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        _instance = new PureDatabase(contextFactory);
        return _instance;
    }

    public void Reset()
    {
        Users.Reset();
        Projects.Reset();
        Resources.Reset();
        Notifications.Reset();
    }

    private PureDatabase(ISqlContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
        Users = new RepositoryEf<User, int>(_contextFactory, UniqueUserRule, GetKeyValue);
        Projects = new RepositoryEf<Project, int>(_contextFactory, UniqueProjectRule, GetKeyValueProject, ProjectConstruct.ConstructShallow);
        Resources = new RepositoryEf<Resource, int>(_contextFactory, UniqueResourceRule, GetKeyValueResource, ResourceConstruct.Construct);
        Tasks = new RepositoryEf<ProjectTask, int>(_contextFactory, UniqueTaskRule, GetKeyValueTask);
        TaskToTasks = new RepositoryEf<TaskToTask, (int, int)>(_contextFactory, UniqueTaskToTaskRule, GetKeyTaskToTaskValue);
        ProjectResourceExclusives = new RepositoryEf<ProjectResourceExclusive, (int, int)>(_contextFactory, UniqueProjResExc, GetKeyValueProjResExc);
        TaskResources = new RepositoryEf<TaskResource, (int, int)>(_contextFactory, UniqueTaskResource, GetKeyValueTaskResource);
        TaskUsers = new RepositoryEf<TaskUser, (int, int)>(_contextFactory, UniqueTaskUserRule, GetKeyValueTaskUser);
        ProjectUsers = new RepositoryEf<ProjectUser, (int, int)>(_contextFactory, UniqueProjectUserRule, GetKeyValueProjectUser);
        Notifications = new RepositoryEf<Notification, int>(_contextFactory, UniqueNotificationRule, GetNotificationKey, NotificationConstruct.Construct);
        return;

        Expression<Func<Notification, bool>> UniqueNotificationRule(Notification not) => not1 => false;
        int GetNotificationKey(Notification not) => not.Id;
        
        Expression<Func<User, bool>> UniqueUserRule(User user) => u => u.Email == user.Email && u.Id != user.Id;
        int GetKeyValue(User user) => user.Id;

        Expression<Func<Project, bool>> UniqueProjectRule(Project project) => p => p.Name == project.Name && p.Id != project.Id;
        int GetKeyValueProject(Project project) => project.Id;

        Expression<Func<Resource, bool>> UniqueResourceRule(Resource resource) => r => r.Name == resource.Name && r.Type == resource.Type && r.Id != resource.Id;
        int GetKeyValueResource(Resource resource) => resource.Id;

        Expression<Func<ProjectTask, bool>> UniqueTaskRule(ProjectTask task) => p => p.Title == task.Title && p.ProjectId == task.ProjectId && p.Id != task.Id;
        int GetKeyValueTask(ProjectTask project) => project.Id;

        Expression<Func<TaskToTask, bool>> UniqueTaskToTaskRule(TaskToTask taskToTask) => t => false;
        (int, int) GetKeyTaskToTaskValue(TaskToTask taskToTask) => (taskToTask.ProjectTaskId, taskToTask.DependentTaskId);

        Expression<Func<ProjectResourceExclusive, bool>> UniqueProjResExc(ProjectResourceExclusive proReExc) => t => false;
        (int, int) GetKeyValueProjResExc(ProjectResourceExclusive proReExc) => (proReExc.ProjectId, proReExc.ResourceId);

        Expression<Func<TaskResource, bool>> UniqueTaskResource(TaskResource taskResource) => t => false;
        (int, int) GetKeyValueTaskResource(TaskResource taskResource) => (taskResource.ResourceId, taskResource.ProjectTaskId);

        Expression<Func<TaskUser, bool>> UniqueTaskUserRule(TaskUser proReExc) => t => false;
        (int, int) GetKeyValueTaskUser(TaskUser taskUser) => (taskUser.ProjectTaskId, taskUser.UserId);

        Expression<Func<ProjectUser, bool>> UniqueProjectUserRule(ProjectUser proUser) => pu => false;
        (int, int) GetKeyValueProjectUser(ProjectUser projectUser) => (projectUser.UserId, projectUser.ProjectId);
    }
}
