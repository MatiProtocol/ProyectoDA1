using Auxiliaries;
using DBContext;
using Domain;
using Domain.HelpersDomain;

namespace DomainConstruct;

public class ProjectConstruct
{
    public static Project ConstructShallow(Project project, SqlContext context)
    {
        project.TaskList = new List<ProjectTask>();
        project.Team = new ProjectTeam();
        project.TaskGraph = new TaskGraph();
        project.ResourceRelation = new Dictionary<string, List<(Resource, int)>>();
        project.ResourceExclusive = new List<(Resource, int)>();
        project.TaskToUser = new Dictionary<string, List<User>>();
        project.UserToTask = new Dictionary<string, List<ProjectTask>>();

        return project;
    }

    public static Project ConstructDeep(Project project, SqlContext context)
    {
        BuildTeam(project, context);
        LoadTasks(project, context);
        BuildTaskGraph(project, context);
        AssignTaskResources(project, context);
        AssignExclusiveResources(project, context);
        MapTaskUsers(project, context);

        return project;
    }

    private static void BuildTeam(Project project, SqlContext context)
    {
        project.Team = new ProjectTeam();

        var membersAux = context.ProjectUser.AsQueryable().Where(pu => pu.ProjectId == project.Id);
        if (!membersAux.Any()) return;

        var manager = membersAux.FirstOrDefault(m => m.Role == Roles.ProjectManager);
        var leader = membersAux.FirstOrDefault(m => m.Role == Roles.ProjectLeader);

        project.Team.Manager = manager == null ? null : context.Users.Find(manager.UserId);
        project.Team.Leader = leader == null ? null : context.Users.Find(leader.UserId);

        var members = membersAux
            .Select(pu => context.Users.First(x => x.Id == pu.UserId))
            .ToDictionary(u => u.Email);

        project.Team.Members = members;
    }

    private static void LoadTasks(Project project, SqlContext context)
    {
        var tasks = context.ProjectTask.AsQueryable().Where(t => t.ProjectId == project.Id).ToList();
        project.TaskList = tasks;
    }

    private static void BuildTaskGraph(Project project, SqlContext context)
    {
        project.TaskGraph = new TaskGraph();
        var tasks = project.TaskList;
        var tasksId = tasks.Select((x) => x.Id).ToArray();

        var taskToTask = context.TaskToTask.AsQueryable()
            .Where(tr => tasksId.Contains(tr.ProjectTaskId) || tasksId.Contains(tr.ProjectTaskId)).ToList();

        foreach (var t in tasks)
        {
            TaskGraphHelper.Add(project.TaskGraph, t);
            var deps = taskToTask.Where(tt => tt.ProjectTaskId == t.Id);

            var depsTuple = new List<(ProjectTask task, TypeDependency typeDependency)>();

            foreach (var tt in deps)
            {
                var task = tasks.FirstOrDefault(projectTask => projectTask.Id == tt.DependentTaskId);
                if (task == null) continue;

                depsTuple.Add((task, tt.TypeDependency));
            }

            foreach (var dep in depsTuple)
            {
                TaskGraphHelper.Add(project.TaskGraph, dep.task);

                TaskGraphHelper.AddDependencyToTask(project.TaskGraph, t, dep.task, dep.typeDependency);
            }
        }
    }

    private static void AssignTaskResources(Project project, SqlContext context)
    {
        project.ResourceRelation = new Dictionary<string, List<(Resource, int)>>();

        var tasks = project.TaskList;
        if (tasks.Count == 0)
            return;

        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var taskIdToTitle = tasks.ToDictionary(t => t.Id, t => t.Title);

        var allResources = context.Resource.ToList();
        if (allResources.Count == 0)
        {
            foreach (var task in tasks)
            {
                project.ResourceRelation[task.Title] = new List<(Resource, int)>();
            }
            return;
        }

        var allTaskResources = context.TaskResource
            .Where(tr => taskIds.Contains(tr.ProjectTaskId))
            .ToList();

        var resourceIdToResource = allResources.ToDictionary(r => r.Id);

        var resourceTask = allTaskResources
            .GroupBy(tr => taskIdToTitle[tr.ProjectTaskId])
            .ToDictionary(
                g => g.Key,
                g => g.Select(tr => (
                    resource: ResourceConstruct.Construct(resourceIdToResource[tr.ResourceId], context),
                    count: tr.Count)).ToList()
            );

        project.ResourceRelation = resourceTask;
    }

    private static void AssignExclusiveResources(Project project, SqlContext context)
    {
        project.ResourceExclusive = [];
        var resources = context.Resource.AsQueryable();

        if (!resources.Any()) return;

        var exclusive = context.ProjectResourceExclusive
            .AsQueryable()
            .Where(pre => pre.ProjectId == project.Id)
            .ToList();

        if (exclusive.Count == 0) return;

        var resourcesExclusive = exclusive
            .Select(pre =>
            {
                var resource = ResourceConstruct.Construct(resources.FirstOrDefault(r => r.Id == pre.ResourceId)!, context);
                return (resource!, pre.Quantity);
            })
            .ToList();

        project.ResourceExclusive = resourcesExclusive;
    }

    private static void MapTaskUsers(Project project, SqlContext context)
    {
        var users = context.Users.AsQueryable();

        var tasks = project.TaskList;
        var tasksId = tasks.Select((x) => x.Id).ToArray();

        var tasksUsers = context.TaskUser.AsQueryable()
            .Where(tu => tasksId.Contains(tu.ProjectTaskId));

        if (!tasksUsers.Any()) return;

        var taskToUser = tasks.ToDictionary(
            task => task.Title,
            task => tasksUsers
                .Where(tu => tu.ProjectTaskId == task.Id)
                .Select(tu => users.First(u => u.Id == tu.UserId))
                .ToList()
        );

        var userToTask = users.ToDictionary(
            user => user.Email,
            user => tasksUsers
                .Where(tu => tu.UserId == user.Id)
                .AsEnumerable()
                .Select(tu => tasks.First(t => t.Id == tu.ProjectTaskId)!)
                .ToList()
        );

        project.TaskToUser = taskToUser;
        project.UserToTask = userToTask;
    }
}




