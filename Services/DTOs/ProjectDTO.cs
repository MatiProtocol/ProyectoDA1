using Domain;
using Auxiliaries;

namespace Services.DTOs;

public class ProjectDTO
{
    public string Name;
    public string Description;
    public DateTime StartDate;
    public DateTime EndDate;
    public List<TaskDTO> Tasks;
    public Tuple<UserDTO?, Dictionary<string, UserDTO>, UserDTO?> Team;
    public Dictionary<string, TaskDTO> Graph;
    private int _id;

    public DateTime Delay = DateTime.Now;
    public Dictionary<string, List<(ResourceDTO resource, int Quantity)>> ResourceRelation;
    public List<(ResourceDTO, int)> ResourceExclucive;
    public Dictionary<string, List<UserDTO>> TaskToUsers = new Dictionary<string, List<UserDTO>>();
    public Dictionary<string, List<TaskDTO>> UserToTask = new Dictionary<string, List<TaskDTO>>();

    public (Project?, Error?) FromEntity()
    {
        try
        {
            var project = new Project(Name, Description)
            {
                Id = Id
            };
            return (project, null);
        }
        catch (Exception e)
        {
            return (null, new Error("ArgumentError", e.Message));
        }
    }

    public static ProjectDTO ToEntity(Project project)
    {
        var taskDtos = TaskListToTaskDTOList(project.TaskList);

        User? managerUser = project.Team.Manager;
        UserDTO? managerDto;
        managerDto = managerUser == null ? null : UserDTO.ToEntity(managerUser);

        User? leaderUser = project.Team.Leader;
        UserDTO? leaderDto;
        leaderDto = leaderUser == null ? null : UserDTO.ToEntity(leaderUser);

        var projectMembers = project.Team.Members;
        var members = MemberListToUserDTOList(projectMembers);

        var team = new Tuple<UserDTO?, Dictionary<string, UserDTO>, UserDTO?>(managerDto, members, leaderDto);

        var resourceDictionary = project.ResourceRelation;
        var resourceRelation = ResourceDicToResourceDTODic(resourceDictionary);

        var resourceExclusiveLst = project.ResourceExclusive;
        var resourceExclucive = ResourceLstToResourceDTOLst(resourceExclusiveLst);

        var preojectDto = new ProjectDTO
        {
            Name = project.Name,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Tasks = taskDtos,
            Graph = new Dictionary<string, TaskDTO>(),
            Team = team,
            ResourceRelation = resourceRelation,
            ResourceExclucive = resourceExclucive,
            TaskToUsers = TaskToUsersTOTaskToUsersDTO(project.TaskToUser),
            UserToTask = UserTOTaskTOUserToTaskDTO(project.UserToTask),
            _id = project.Id,
            Delay = project.Delay
        };
        return preojectDto;
    }

    private static List<UserDTO> UserListTOUserDTOList(List<User> userList)
    {
        var newList = new List<UserDTO>();

        foreach (var user in userList) newList.Add(UserDTO.ToEntity(user));

        return newList;
    }

    private static Dictionary<string, List<UserDTO>> TaskToUsersTOTaskToUsersDTO(Dictionary<string, List<User>> TaskToUser)
    {
        var newDic = new Dictionary<string, List<UserDTO>>();

        foreach (var TaskUser in TaskToUser)
            newDic.Add(TaskUser.Key, UserListTOUserDTOList(TaskUser.Value));

        return newDic;
    }

    private static Dictionary<string, List<TaskDTO>> UserTOTaskTOUserToTaskDTO(Dictionary<string, List<ProjectTask>> UserToTask)
    {
        var newDic = new Dictionary<string, List<TaskDTO>>();

        foreach (var UserTask in UserToTask)
            newDic.Add(UserTask.Key, TaskListToTaskDTOList(UserTask.Value));

        return newDic;
    }

    private static List<TaskDTO> TaskListToTaskDTOList(List<ProjectTask> tasks)
    {
        List<TaskDTO> taskDtos = new List<TaskDTO>();
        foreach (var task in tasks) taskDtos.Add(TaskDTO.ToEntity(task));
        return taskDtos;
    }

    private static Dictionary<string, UserDTO> MemberListToUserDTOList(Dictionary<string, User> projectMembers)
    {
        Dictionary<string, UserDTO> members = new Dictionary<string, UserDTO>();

        foreach (var member in projectMembers.Values) members.Add(member.Email, UserDTO.ToEntity(member));
        return members;
    }

    private static Dictionary<string, List<(ResourceDTO, int)>> ResourceDicToResourceDTODic(
        Dictionary<string, List<(Resource, int)>> resourceDictionary)
    {
        var dict = new Dictionary<string, List<(ResourceDTO, int)>>();

        foreach (var stringList in resourceDictionary)
        {
            var list = new List<(ResourceDTO, int)>();
            foreach (var tuple in stringList.Value)
            {
                list.Add((ResourceDTO.ToEntity(tuple.Item1), tuple.Item2));
            }
            dict.Add(stringList.Key, list);
        }

        return dict;
    }

    private static List<(ResourceDTO, int)> ResourceLstToResourceDTOLst(List<(Resource, int)> resourceLst)
    {
        var resourceDtoLst = new List<(ResourceDTO, int)>();
        foreach (var resource in resourceLst) resourceDtoLst.Add((ResourceDTO.ToEntity(resource.Item1), resource.Item2));
        return resourceDtoLst;
    }

    public int Id
    {
        get => _id;
    }
}
