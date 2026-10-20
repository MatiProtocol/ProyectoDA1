using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Domain.HelpersDB;
using DomainConstruct;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.IdentityModel.Tokens;
using Services.DTOs;
using Timer = System.Timers.Timer;

namespace Services;

public class ProjectService
{

    private PureDatabase _database = PureDatabase.GetInstance();
    private static ProjectService? _instance;
    private ISqlContextFactory _contextFactory;
    public static int Interval = 10 * 60 * 1000;
    public static double SpanTime = 5;
    private static Timer _aTimer;

    public static ProjectService GetInstance(ISqlContextFactory? contextFactory = null)
    {

        if (_instance == null)
        {
            _instance = new ProjectService(contextFactory);
            _instance.GenerateInterval();

            foreach (var project in _instance.GetAllProjects())
            {
                Reactivity.GetInstance().OnChangeTaskGraph += () => _instance.GetUpdateProjectState(ProjectDTO.ToEntity(project));
            }
        }
        _instance._database = PureDatabase.GetNewInstance(contextFactory);
        return _instance;
    }

    public static ProjectService GetNewInstance(ISqlContextFactory? contextFactory = null)
    {

        var instance = new ProjectService(contextFactory);
        instance._database = PureDatabase.GetNewInstance(contextFactory);
        instance.GenerateInterval();
        return instance;
    }

    private void GenerateInterval()
    {

        _aTimer = new Timer();
        _aTimer.Interval = Interval;

        _aTimer.Elapsed += (_, _) =>
        {
            foreach (var p in GetAllProjects())
            {
                GetUpdateProjectState(ProjectDTO.ToEntity(p), false);
            }
        };

        _aTimer.Enabled = true;
    }

    private ProjectService(ISqlContextFactory? ctxFactory = null)
    {
        if (ctxFactory == null) ctxFactory = new SqlContextFactory();
        _contextFactory = ctxFactory;
    }

    public Error? CreateProject(UserDTO solicitant, ProjectDTO newProjectDto)
    {
        var userService = UserService.GetNewInstance(_contextFactory);

        var solicitantUser = userService.GetUserDto(solicitant.Email);
        if (solicitantUser == null) return new Error(Error.Types.KeyDoesNotExist, "Usurio no existe");

        if (solicitantUser.Role == Roles.ProjectMember)
            return new Error("ArgumentError", "Applicant must be a system manager or project manager");

        var (project, error) = newProjectDto.FromEntity();
        if (project == null) return error;

        if (_database.Projects.Add(project) != null) return new Error(Error.Types.KeyAlreadyExist, "Project already exists");

        Reactivity.GetInstance().OnChangeTaskGraph += () => GetUpdateProjectState(newProjectDto);
        return null;
    }

    private (Project? project, User? user, Error? error) HasPermissionToEdit(UserDTO applicant, string projectDtoName)
    {
        var userService = UserService.GetNewInstance(_contextFactory);

        var (user, _) = userService.GetUser(applicant.Email);
        if (user == null) return (null, null, new Error(Error.Types.KeyDoesNotExist, "Aplicante no existe"));

        if (user.Role == Roles.ProjectMember)
            return (null, user, new Error(Error.Types.PermissionDenied, "Permisos insuficientes"));

        var project = GetProject(projectDtoName).Item1;
        if (project == null) return (null, user, new Error(Error.Types.KeyDoesNotExist, "El Proyecto no existe"));


        var hasRightsTo = _database.ProjectUsers.FindBy(pu => pu.UserId == user.Id && pu.ProjectId == project.Id);

        if (user.Role != Roles.SystemManager && (hasRightsTo == null || hasRightsTo.Role < Roles.ProjectManager))
            return (null, user, new Error(Error.Types.PermissionDenied, "No eres el administrador de este proyecto"));

        return (project, user, null);
    }

    public bool DeleteProject(UserDTO userDto, string projectDtoName)
    {
        var (project, _, _) = HasPermissionToEdit(userDto, projectDtoName);
        if (project == null) return false;

        int projectId = project.Id;

        var exclusives = _database.ProjectResourceExclusives.Query()
            .AsEnumerable()
            .Where(pre => pre.ProjectId == projectId)
            .Select(pre => pre.ResourceId);

        var tasksWithProject = _database.Tasks.
            Query(context => context.ProjectTask.Where(pt => pt.ProjectId == projectId)).Select(t => t.Id);

        foreach (var taskId in tasksWithProject)
        {
            var dependencyRemove = _database.TaskToTasks.Query(context =>
                context.TaskToTask.Where(tt => tt.ProjectTaskId == taskId || tt.DependentTaskId == taskId));
            foreach (var dependency in dependencyRemove)
            {
                _database.TaskToTasks.Remove((dependency.ProjectTaskId, dependency.DependentTaskId));
            }

            var resourceRemove = _database.TaskResources.Query(context =>
                context.TaskResource.Where(tt => tt.ProjectTaskId == taskId));

            foreach (var resource in resourceRemove)
            {
                _database.TaskToTasks.Remove((resource.ResourceId, resource.ProjectTaskId));
            }
        }

        var resourceService = ResourceService.GetNewInstance(_contextFactory);

        foreach (var resourceExclusive in exclusives)
        {
            resourceService.DeleteResource(userDto, resourceExclusive);
        }
        Reactivity.GetInstance().OnChangeTaskGraph -= () => GetUpdateProjectState(ProjectDTO.ToEntity(project));
        
        return _database.Projects.Remove(project.Id);
    }

    public (ProjectDTO?, Error?) GetProjectDto(string projectDtoName)
    {
        var (project, error) = GetProject(projectDtoName);
        return project == null
            ? (null, error)
            : (ProjectDTO.ToEntity(ProjectConstruct.ConstructDeep(project, _contextFactory.Generate())), error);
    }

    public (Project?, Error?) GetProject(string projectName)
    {
        var project = GetProjectByName(projectName);
        return (project, project == null ? new Error(Error.Types.KeyDoesNotExist, "Proyecto no existe") : null);
    }

    private Project? GetProjectByName(string name)
    {
        return _database.Projects.FindBy(p => p.Name == name);
    }

    public Error? ChangeNameProject(UserDTO applicant, string projectDtoName, string newName)
    {
        var (_, _, error) = HasPermissionToEdit(applicant, projectDtoName);
        if (error != null) return error;

        if (newName == "") return new Error("ArgumentError", "New project name cannot be empty");

        var exist = _database.Projects.FindBy(p => p.Name == newName);
        if (exist != null) return new Error(Error.Types.KeyDoesNotExist, "Nuevo nombre no esta disponible");

        var project = GetProject(projectDtoName).Item1;
        project!.Name = newName;

        return _database.Projects.Update(project);
    }

    public Error? ChangeDescProject(UserDTO applicant, string projectDtoName, string newDesc)
    {
        var (project, _, error) = HasPermissionToEdit(applicant, projectDtoName);
        if (project == null) return error;

        if (newDesc == "") return new Error("ArgumentError", "New project description cannot be empty");

        project.Description = newDesc;
        return _database.Projects.Update(project);
    }

    public Error? AddTask(UserDTO userDto, ProjectDTO projectDto, TaskDTO taskDto)
    {
        var (project, _, error) = HasPermissionToEdit(userDto, projectDto.Name);
        if (project == null) return error;

        (var task, error) = taskDto.FromEntity();
        if (task == null) return error;

        var taskWithThatTittle = _database.Tasks.Query(context =>
            context.ProjectTask
                .Where(t => t.ProjectId == project.Id && t.Title == task.Title)
        );

        if (taskWithThatTittle.Any())
            return new Error(Error.Types.KeyAlreadyExist, "Nombre de la tarea ya esta siendo utilizado");

        task.ProjectId = project.Id;

        var errorAdd = _database.Tasks.Add(task);

        if (errorAdd != null) return errorAdd;
        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public Error? RemoveTask(UserDTO userDto, ProjectDTO projectDto, TaskDTO taskDto)
    {
        var (project, _, error) = HasPermissionToEdit(userDto, projectDto.Name);
        if (project == null) return error;

        var task = _database.Tasks.FindBy(task => task.Id == taskDto.Id);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        var dependencyRemove = _database.TaskToTasks.Query(context =>
            context.TaskToTask.Where(tt => tt.ProjectTaskId == task.Id || tt.DependentTaskId == task.Id));
        foreach (var dependency in dependencyRemove)
        {
            _database.TaskToTasks.Remove((dependency.ProjectTaskId, dependency.DependentTaskId));
        }

        var resourceRemove = _database.TaskResources.Query(context =>
            context.TaskResource.Where(tt => tt.ProjectTaskId == task.Id));

        foreach (var resource in resourceRemove)
        {
            _database.TaskToTasks.Remove((resource.ResourceId, resource.ProjectTaskId));
        }
        
        if (!_database.Tasks.Remove(task.Id)) return new Error("KeyDoesNotExist", "No se pudo eliminar la tarea");
        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public bool IsManagerOfThisProject(UserDTO userDto, ProjectDTO projectDto)
    {
        return _database.ProjectUsers.Exists(pu => pu.UserId == userDto.Id && pu.ProjectId == projectDto.Id && pu.Role == Roles.ProjectManager);
    }

    public List<TaskDTO> GetTasksByProject(ProjectDTO projectDto)
    {
        return _database.Tasks.Query(context =>
            context.ProjectTask
                .Join(
                    context.Project.Where(p => p.Name == projectDto.Name),
                    task => task.ProjectId,
                    project => project.Id,
                    (task, p) => task
                )
        )
        .AsEnumerable()
        .Select(TaskDTO.ToEntity)
        .ToList();
    }

    public Error? AddManagerToProject(UserDTO applicantDto, UserDTO userDto, ProjectDTO projectDto)
    {
        var (project, applicant, errorPermissions) = HasPermissionToEdit(applicantDto, projectDto.Name);
        if (project == null || applicant == null) return errorPermissions;

        var userService = UserService.GetNewInstance(_contextFactory);
        var (user, errorUser) = userService.GetUser(userDto.Email);
        if (user == null) return errorUser;

        if (_database.ProjectUsers.Exists(pu => pu.ProjectId == project.Id && pu.Role == Roles.ProjectManager))
            return new Error(Error.Types.KeyAlreadyExist, "Ya hay un administrador de proyectos");

        var errorCanManageProject = VerifyIfCanManageProject(userDto.Email);
        if (errorCanManageProject != null) return errorCanManageProject;

        var projectUser = new ProjectUser
        {
            UserId = user.Id,
            ProjectId = project.Id,
            Role = Roles.ProjectManager
        };

        return _database.ProjectUsers.Add(projectUser);
    }

    private Error? VerifyIfCanManageProject(string email)
    {
        var userService = UserService.GetNewInstance(_contextFactory);

        var (manager, _) = userService.GetUser(email);
        if (manager == null) return new Error("KeyDoesNotExist", "El usuario no existe");

        var projects = GetAllProjectThatUserAreManager(UserDTO.ToEntity(manager));
        if (projects.Length != 0) return new Error(Error.Types.PermissionDenied, "El usuario ya es administrador de un proyecto");

        if (manager.Role < Roles.ProjectManager)
            return new Error(Error.Types.PermissionDenied,
                "Usuario no tiene suficientes permisos para ser administrador de proyecto");
        return null;
    }

    public Error? AddMemberToProject(UserDTO applicant, UserDTO userDto, ProjectDTO projectDto)
    {
        var (project, _, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (error != null) return error;

        if (IsPartOfTheProject(userDto, projectDto))
            return new Error("KeyAlreadyExist", "El usuario ya es miembro del proyecto");

        var projectUser = new ProjectUser
        {
            Role = Roles.ProjectMember,
            UserId = userDto.Id,
            ProjectId = project!.Id
        };

        return _database.ProjectUsers.Add(projectUser);
    }

    public bool IsMemberOfThisProject(UserDTO userDto, ProjectDTO projectDto)
    {
        var role = _database.ProjectUsers.FindBy(pu => pu.UserId == userDto.Id && pu.ProjectId == projectDto.Id);

        return role != null;
    }

    public bool IsLeaderOfThisProject(UserDTO userDto, ProjectDTO projectDto)
    {
        return _database.ProjectUsers.Exists(pu => pu.UserId == userDto.Id && pu.ProjectId == projectDto.Id && pu.Role == Roles.ProjectLeader);
    }

    public ProjectDTO[] GetAllProjectsDto()
    {
        var projectsDto = _database.Projects.Query()
            .AsEnumerable()
            .Select(ProjectDTO.ToEntity)
            .ToArray();

        return projectsDto;
    }

    private Project[] GetAllProjects()
    {
        var projectsDb = _database.Projects.GetAll();
        return projectsDb.Select(p => GetProject(p.Name).Item1).ToArray()!;
    }

    public ProjectDTO[] GetProjectsByUser(UserDTO userDto)
    {
        var userService = UserService.GetNewInstance(_contextFactory);

        var user = userService.GetUser(userDto.Email).Item1;
        if (user == null) return [];

        var userId = user.Id;

        var projects = _database.Projects.Query((context) =>
                context.Project
                    .Join(context.ProjectUser.Where(pu => pu.UserId == user.Id), p => p.Id, pu => pu.ProjectId, (p, pu) => new { p, pu })
                    .Select(t => t.p)
            ).AsEnumerable()
            .Select(ProjectDTO.ToEntity)
            .ToArray();

        return projects;
    }

    public Error? RemoveManagerFromProject(UserDTO applicant, ProjectDTO projectDto)
    {
        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        _database.ProjectUsers.Remove((user.Id, project.Id));
        return null;
    }

    public Error? RemoveMemberFromProject(UserDTO applicant, UserDTO member, ProjectDTO projectDto)
    {
        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null) return error;

        if (_database.ProjectUsers.Exists(pu => pu.UserId == member.Id && pu.ProjectId == project.Id && pu.Role != Roles.ProjectMember))
            return new Error(Error.Types.ArgumentError, "De esta forma no se puede eliminar al manager");

        if (!IsMemberOfThisProject(member, projectDto))
            return new Error(Error.Types.KeyDoesNotExist, "El usuario no es miembro de este proyecto.");

        return _database.ProjectUsers.Remove(( member.Id,project.Id)) ? null : new Error(Error.Types.DatabaseRemoveError, "No se a posido eliminar al usurio del proyecto");
    }

    public (TaskDTO[]?, Error?) GetTaskCanStart(ProjectDTO projectDto)
    {
        var tasks = new List<TaskDTO>();

        var project = GetProject(projectDto.Name).Item1;
        if (project == null) return (null, new Error("KeyDoesNotExist", "El proyecto no existe"));

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var taskGraphService = new TaskGraphService(project, _contextFactory);
        foreach (var task in project.TaskList)
        {
            if (task.State == TaskState.Pending && taskGraphService.ValidTaskToStart(task))
            {
                tasks.Add(TaskDTO.ToEntity(task));
            }
        }

        return (tasks.ToArray(), null);
    }

    public Error? AddMemberToTask(UserDTO applicant, ProjectDTO projectDto, TaskDTO taskDto, UserDTO targetDto)
    {
        var (project, _, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null) return error;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        if (!IsMemberOfThisProject(targetDto, projectDto))
            return new Error(Error.Types.PermissionDenied, "Usuario no pertence en el proyecto");

        var (target, _) = UserService.GetNewInstance(_contextFactory).GetUser(targetDto.Email);
        if (target == null) return new Error("KeyDoesNotExist", "El usuario no existe");

        var task = _database.Tasks.FindBy(task => task.ProjectId == project.Id && task.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        project.AssignedUserToTask(task, target);

        var taskUser = new TaskUser
        {
            ProjectTaskId = task.Id,
            UserId = target.Id
        };

        return _database.TaskUsers.Add(taskUser);
    }

    public Error? RemoveMemberFromTask(UserDTO applicant, ProjectDTO projectDto, TaskDTO taskDto, UserDTO targetDto)
    {
        var (project, _, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null) return error;

        if (!IsMemberOfThisProject(targetDto, projectDto))
            return new Error(Error.Types.PermissionDenied, "Usuario no pertence en el proyecto");

        var userService = UserService.GetNewInstance(_contextFactory);

        var target = userService.GetUser(targetDto.Email).Item1!;

        var task = _database.Tasks.FindBy(task => task.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        if (!_database.TaskUsers.Remove((task.Id, target.Id)))
            return new Error("KeyDoesNotExist", "No se pudo eliminar el usuario");

        return null;
    }

    public struct ChangeTaskHelper
    {
        public TaskDTO OriginalTaskDto;
        public ProjectDTO ProjectDto;
        public TaskDTO NewInfoTaskDto;
    }

    public Error? ChangeTaskTitle(UserDTO applicant, ChangeTaskHelper changeInfo)
    {
        if (changeInfo.OriginalTaskDto.Title.Equals(changeInfo.NewInfoTaskDto.Title)) return null;

        var projectDto = changeInfo.ProjectDto!;
        var taskDto = changeInfo.OriginalTaskDto;
        var newInfoTaskDto = changeInfo.NewInfoTaskDto;

        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        var task = _database.Tasks.FindBy(t => t.ProjectId == project.Id && t.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        var oldTitle = task.Title;

        Error? errorSetTitle = null;
        try
        {
            task.Title = newInfoTaskDto.Title;
        }
        catch (Exception e)
        {
            errorSetTitle = new Error(Error.Types.ArgumentError, e.Message);
        }

        if (errorSetTitle != null) return errorSetTitle;

        return _database.Tasks.Update(task);
    }

    public Error? ChangeTaskDescription(UserDTO applicant, ChangeTaskHelper changeInfo)
    {
        var projectDto = changeInfo.ProjectDto;
        var taskDto = changeInfo.OriginalTaskDto;
        var newInfoTaskDto = changeInfo.NewInfoTaskDto;

        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        var task = _database.Tasks.FindBy(t => t.ProjectId == project.Id && t.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        task.Description = newInfoTaskDto.Description;

        return _database.Tasks.Update(task);
    }

    public Error? ChangeTaskDuration(UserDTO applicant, ChangeTaskHelper changeInfo)
    {
        var projectDto = changeInfo.ProjectDto;
        var taskDto = changeInfo.OriginalTaskDto;
        var newInfoTaskDto = changeInfo.NewInfoTaskDto;

        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        var task = _database.Tasks.FindBy(t => t.ProjectId == project.Id && t.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        task.DurationHours = newInfoTaskDto.DurationHour;

        if (_database.Tasks.Update(task) != null) return new Error("KeyDoesNotExist", "No se pudo actualizar la tarea");
        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public Error? ChangeTaskState(UserDTO applicant, ChangeTaskHelper changeInfo)
    {
        if (changeInfo.OriginalTaskDto.State > changeInfo.NewInfoTaskDto.State)
            return new Error("StateCannotBeChanged", "El estado no puede cambiar a un estado anterior");

        var projectDto = changeInfo.ProjectDto;
        var taskDto = changeInfo.OriginalTaskDto;
        var newInfoTaskDto = changeInfo.NewInfoTaskDto;

        var (project, error) = GetProject(projectDto.Name);
        if (project == null) return error;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var task = _database.Tasks.FindBy(t => t.ProjectId == project.Id && t.Title == taskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        error = IsMemberTask(project, task, applicant);
        if (error != null && !IsManagerOfThisProject(applicant, projectDto)) return error;

        var taskGraphService = new TaskGraphService(project, _contextFactory);

        if (!taskGraphService.ValidTaskToStart(task))
            return new Error(Error.Types.PermissionDenied, "La tarea no puede ser iniciada ya que depende de otra");

        if (newInfoTaskDto.State <= TaskState.Started)
        {
            var usedManyTimes = project.ResourceRelation.ContainsKey(task.Title) && project.ResourceRelation[task.Title].Any(resource =>
                resource.Item2 > resource.Item1.Quantity - ResourceService.GetNewInstance(_contextFactory)
                    .GetResourceTotalUses(ResourceDTO.ToEntity(resource.Item1)));

            if (usedManyTimes)
            {
                return new Error("ArgumentError", "La cantidad de recursos a asignar es mayor a la disponible");
            }
            task.SetStartTaskDate(newInfoTaskDto.StartTaskDate);
        }
        else task.SetEndTaskDate(newInfoTaskDto.EndTaskDate);

        _database.Tasks.Update(task);
        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    private Error? IsMemberTask(Project project, ProjectTask task, UserDTO userDto)
    {
        var isMember = _database.TaskUsers.FindBy(tu => tu.UserId == userDto.Id && tu.ProjectTaskId == task.Id);
        if (isMember != null) return null;
        return new Error(Error.Types.PermissionDenied, "No tienes asociación con la tarea");
    }

    public Error? AddDependencyToTask(UserDTO applicant, ProjectDTO projectDto, TaskGraphService.Edge edge)
    {
        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var target = project.TaskList.Find(task => task.Title == edge.targetDto.Title);
        if (target == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        if (target.State != TaskState.Pending) return new Error(Error.Types.PermissionDenied, "La tarea ya fue completada");

        var dep = project.TaskList.Find(task => task.Title == edge.depDto.Title);
        if (dep == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        var taskRelation = new TaskToTask
        {
            ProjectTaskId = target.Id,
            DependentTaskId = dep.Id,
            TypeDependency = edge.type
        };

        var graphService = new TaskGraphService(project, _contextFactory);

        var verifyNewEdge = graphService.VerifyValidNewEdge(target, dep);
        if (verifyNewEdge != null) return verifyNewEdge;

        if (_database.TaskToTasks.Add(taskRelation) != null) return new Error("KeyAlreadyExist", "Esta relacion ya existe");

        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public Error? RemoveDependencyFromTask(UserDTO applicant, ProjectDTO projectDto, TaskDTO targetDto, TaskDTO depDto)
    {
        var (project, user, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null || user == null) return error;

        var target = _database.Tasks.FindBy(t => t.Id == targetDto.Id);
        if (target == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        var dep = _database.Tasks.FindBy(t => t.Id == depDto.Id);
        if (dep == null) return new Error("KeyDoesNotExist", "La tarea no existe");

        if (!_database.TaskToTasks.Exists(p => p.ProjectTaskId == target.Id && p.DependentTaskId == dep.Id))
            return new Error("KeyDoesNotExist", "Esta relacion no existe");

        if (!_database.TaskToTasks.Remove((target.Id, dep.Id)))
            return new Error("KeyDoesNotExist", "No se pudo eliminar la relacion");

        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public (TypeDependency type, TaskDTO task)[]? GetTaskDependencies(ProjectDTO projectDto, TaskDTO taskDto)
    {
        var (project, _) = GetProject(projectDto.Name);
        if (project == null) return null;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var task = project.TaskList.Find(task => task.Title == taskDto.Title);
        if (task == null) return null;

        var taskGraph = new TaskGraphService(project, _contextFactory!);

        return taskGraph.GetTaskDependencies(task).Select(t => (type: t.type, task: TaskDTO.ToEntity(t.task))).ToArray();
    }

    public bool TaskDependencyVerifyEdge(ProjectDTO projectDto, TaskDTO targetDto, TaskDTO depDto)
    {
        var (project, _) = GetProject(projectDto.Name);

        if (project == null) return false;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var target = project.TaskList.Find(task => task.Title == targetDto.Title);
        if (target == null || target.State == TaskState.Finished) return false;

        var dep = project.TaskList.Find(task => task.Title == depDto.Title);
        if (dep == null) return false;

        var taskGraph = new TaskGraphService(project, _contextFactory);

        return taskGraph.VerifyValidNewEdge(target, dep) == null;
    }

    public (TaskDTO[] path, int duration) GetCriticalPath(ProjectDTO projectDto)
    {
        var (project, _) = GetProject(projectDto.Name);
        if (project == null) return ([], 0);

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        GetUpdateProjectState(projectDto);

        var taskGraph = new TaskGraphService(project, _contextFactory);
        var criticalPath = taskGraph.GetCriticalPath();
        return (criticalPath.path.Select(TaskDTO.ToEntity).ToArray(), criticalPath.duration);
    }

    private struct TimeHelper
    {
        public string state;
        public int days;
        public int hours;
        public int minutes;
        public int seconds;
        public int miliseconds;
        public int nanoseconds;
        public int microseconds;
    }

    private void SendDelayNotification(Project project, TaskGraphService taskGraph, bool update)
    {
        var (path, duration) = taskGraph.GetCriticalPath();
        var endDate = TaskGraphService.GetDateTimeNow().AddHours(duration);

        if (path.Length > 0 && path[0].State == TaskState.Started && ((TaskGraphService.GetDateTimeNow() - path[0].StartTaskDate).TotalHours - path[0].DurationHours) <= 0) endDate = endDate.AddMinutes(-(endDate - project.EndDate).TotalMinutes);

        if (Math.Abs((endDate - project.Delay).TotalMinutes) < SpanTime) return;

        var state = project.EndDate.CompareTo(endDate) < 0 ? "atraso" : "adelanto";
        var diff = project.EndDate - endDate;
        var timeHelper = new TimeHelper
        {
            state = state,
            days = Math.Abs(diff.Days),
            hours = Math.Abs(diff.Hours),
            minutes = Math.Abs(diff.Minutes),
            seconds = Math.Abs(diff.Seconds),
            miliseconds = Math.Abs(diff.Milliseconds),
            nanoseconds = Math.Abs(diff.Nanoseconds),
            microseconds = Math.Abs(diff.Microseconds),
        };

        if (update) project.EndDate = endDate;
        project.Delay = endDate;


        if (project.Team.Manager != null) Notify(project.Team.Manager, project, timeHelper);
        if (project.Team.Leader != null) Notify(project.Team.Leader, project, timeHelper);

        foreach (var user in project.Team.Members) Notify(user.Value, project, timeHelper);

        _database.Projects.Update(project);
    }

    private void Notify(User user, Project project, TimeHelper th)
    {
        var notificationService = NotificationService.GetNewInstance(_contextFactory);
        var message =
            $"Proyecto {project.Name} se {th.state} por {th.days}d {th.hours}h {th.minutes}m {th.seconds}s {th.miliseconds} ms {th.microseconds} μs {th.nanoseconds} ns";
        notificationService.SendNotification(user, message);
    }

    public bool CalculateEstimation(ProjectDTO projectDto, bool update = true)
    {
        var (project, _) = GetProject(projectDto.Name);
        if (project == null) return false;
        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        var taskGraph = new TaskGraphService(project, _contextFactory);

        SendDelayNotification(project, taskGraph, update);

        return true;
    }

    public TaskState GetUpdateProjectState(ProjectDTO projectDto, bool update = true)
    {
        var (project, _) = GetProject(projectDto.Name);
        if (project == null) return TaskState.Pending;

        var context = _contextFactory.Generate();
        var tasks = context.ProjectTask.Where(t => t.ProjectId == project.Id).ToList();
        var dic = tasks.GroupBy(t => t.State)
            .ToDictionary(g => g.Key, g => g.ToList());

        var started = dic.ContainsKey(TaskState.Started) ? dic[TaskState.Started].Count : 0;
        var finished = dic.ContainsKey(TaskState.Finished) ? dic[TaskState.Finished].Count : 0;

        if (dic.ContainsKey(TaskState.Planed))
            foreach (var taskPlaned in dic[TaskState.Planed])
            {
                if (taskPlaned.StartTaskDate.CompareTo(TaskGraphService.GetDateTimeNow()) < 0)
                {
                    taskPlaned.SetStartTaskDate(taskPlaned.StartTaskDate);
                    started++;
                }
            }

        if (started == finished && started == 0)
        {
            project.StartDate = DateTime.Now;
            _database.Projects.Update(project, false);
            CalculateEstimation(projectDto, update);
            
            return TaskState.Pending;
        }

        if (finished != tasks.Count)
        {
            CalculateEstimation(projectDto, update);
            return TaskState.Started;
        }

        if (project.EndDate.CompareTo(DateTime.Now) > 0)
        {
            project.EndDate = DateTime.Now;
            _database.Projects.Update(project);
        }

        return TaskState.Finished;
    }

    public Error? SetLeaderToProject(UserDTO applicant, UserDTO leaderDto, ProjectDTO projectDto)
    {
        var (project, _, error) = HasPermissionToEdit(applicant, projectDto.Name);
        if (project == null) return error;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        if (project.Team.Leader != null)
        {
            var errorLeader = RemoveLeaderToProject(applicant, projectDto);
            if (errorLeader != null) return errorLeader;
            project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());
        }

        if (project.Team.Members.ContainsKey(leaderDto.Email))
        {
            var errorMember = RemoveMemberFromProject(applicant, leaderDto, projectDto);
            if (errorMember != null) return errorMember;
            project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());
        }

        var userService = UserService.GetNewInstance(_contextFactory);

        var (user, _) = userService.GetUser(leaderDto.Email);
        if (user == null) return new Error(Error.Types.KeyDoesNotExist, "El usuario no existe");

        if (user.Role < Roles.ProjectLeader)
            return new Error(Error.Types.PermissionDenied, "El usuario no tiene permisos para ser el lider del proyecto");

        project.Team.Leader = user;

        var projectUser = new ProjectUser
        {
            UserId = user.Id,
            ProjectId = project.Id,
            Role = Roles.ProjectLeader
        };

        _database.ProjectUsers.Add(projectUser);
        return null;
    }

    public Error? RemoveLeaderToProject(UserDTO applicantDto, ProjectDTO projectDto)
    {
        var (project, applicant, error) = HasPermissionToEdit(applicantDto, projectDto.Name);
        if (project == null || applicant == null) return error;

        var user = _database.Users.Query(context =>
            context.Users
                .Join(
                    context.ProjectUser.Where(pu => pu.ProjectId == project.Id && pu.Role == Roles.ProjectLeader),
                    u => u.Id,
                    pu => pu.UserId,
                    (u, pu) => u
                        )
        ).ToArray();

        var projectUser = _database.ProjectUsers.FindBy(pu => pu.ProjectId == project.Id && pu.Role == Roles.ProjectLeader);
        projectUser!.Role = Roles.ProjectMember;

        _database.ProjectUsers.Update(projectUser);

        return null;
    }

    public UserDTO[] GetUsersCanBeLeader(ProjectDTO projectDto)
    {

        if (!_database.Projects.Exists(p => p.Id == projectDto.Id)) return [];

        return _database.Users.Query(context =>
            context.Users
            .Where(u => u.Role >= Roles.ProjectLeader &&
                        !context.ProjectUser.Any(pu =>
                            pu.ProjectId == projectDto.Id &&
                            pu.UserId == u.Id &&
                            pu.Role == Roles.ProjectManager))
            )
            .AsEnumerable()
            .Select(UserDTO.ToEntity)
            .ToArray();
    }

    public bool IsPartOfTheProject(UserDTO userDto, ProjectDTO projectDto)
    {
        return IsManagerOfThisProject(userDto, projectDto)
               ||
               IsMemberOfThisProject(userDto, projectDto)
               ||
               IsLeaderOfThisProject(userDto, projectDto);
    }

    public bool IsManagerOrLeader(UserDTO userDto, ProjectDTO projectDto)
    {
        return IsManagerOfThisProject(userDto, projectDto)
               || IsLeaderOfThisProject(userDto, projectDto);
    }

    private struct ProjectExporterInfo
    {
        public string _name { get; set; }

        [JsonIgnore]
        public DateTime _startDate { get; set; }

        [JsonPropertyName("startDate")]
        public string _startDateJson => _startDate.ToString("MM/dd/yyyy");
        public TasksExporterInfo[] _tasks { get; set; }
    }

    private struct TasksExporterInfo
    {
        public string _name { get; set; }
        public int duration { get; set; }

        [JsonIgnore]
        public DateTime _startDate { get; set; }

        [JsonPropertyName("startDate")]
        public string _startDateJson => _startDate.ToString("MM/dd/yyyy");

        public ResourceExporterInfo[] _resources { get; set; }
    }

    private struct ResourceExporterInfo
    {
        public int _count { get; set; }
        public string _name { get; set; }
    }

    public struct DownloaderProjectInfo
    {
        public string Content;
        public string MimeType;
        public string FileName;

    }

    public (DownloaderProjectInfo?, Error?) ExportProject(UserDTO userDto, bool JsonFormat)
    {
        var userService = UserService.GetNewInstance(_contextFactory);

        DownloaderProjectInfo? result;

        var (user, _) = userService.GetUser(userDto.Email);
        if (user == null) return (null, new Error("DefaultError", "El usuario no existe"));

        if (user.Role < Roles.ProjectLeader)
            return (null, new Error("DefaultError", "El usuario no tiene permisos para exportar proyectos"));

        var projects = AllProjectThatUserAreLeader(userDto)
            .Select(p => ProjectConstruct.ConstructDeep(p, _contextFactory.Generate()))
            .OrderBy(p => p.StartDate).ToArray();

        if (projects.Length == 0) return (null, new Error("DefaultError", "No tienes proyectos para exportar"));

        var projectsExports = GetProjectExportInfo(projects);

        if (JsonFormat)
        {
            var json = JsonSerializer.Serialize(projectsExports, new JsonSerializerOptions { WriteIndented = true });

            result = new DownloaderProjectInfo
            {
                Content = json,
                MimeType = "application/json",
                FileName = "projects.json"
            };
            return (result, null);
        }
        var lines = new List<string>();

        foreach (var project in projectsExports)
        {
            lines.Add($"Proyecto: {project._name}, Comienzo: {project._startDate:MM/dd/yyyy}");

            foreach (var task in project._tasks)
            {
                lines.Add($"Tarea: {task._name}, Comienzo: {task._startDate:MM/dd/yyyy}, Duracion: {task.duration}");
                lines.AddRange(task._resources.Select(resource => $"Recurso: {resource._name} {resource._count} "));
                lines.Add("");
            }
            lines.Add("");
        }
        var csv = string.Join(Environment.NewLine, lines);

        result = new DownloaderProjectInfo
        {
            Content = csv,
            MimeType = "application/csv",
            FileName = "projects.csv"
        };
        return (result, null);

    }

    private List<ProjectExporterInfo> GetProjectExportInfo(Project[] projects)
    {
        List<ProjectExporterInfo> result = [];

        foreach (var project in projects)
        {
            var resourceTasks = GetResourceTaskInfoExporter(project)
                .OrderBy(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var tasksExporterInfoList = new List<TasksExporterInfo>();

            foreach (var (taskTitle, value) in resourceTasks)
            {
                var (startDate, duration, resourceList) = value;

                var resourcesExporterInfo =
                    resourceList.Select(r => new ResourceExporterInfo
                    {
                        _name = r.Item1,
                        _count = r.Item2
                    }).ToArray();

                var taskExporter = new TasksExporterInfo
                {
                    _name = taskTitle,
                    _startDate = startDate,
                    duration = duration,
                    _resources = resourcesExporterInfo
                };

                tasksExporterInfoList.Add(taskExporter);
            }

            var projectExporterInfo = new ProjectExporterInfo
            {
                _name = project.Name,
                _startDate = project.StartDate,
                _tasks = tasksExporterInfoList.ToArray()
            };

            result.Add(projectExporterInfo);
        }

        return result;
    }

    private Dictionary<string, (DateTime, int, List<(string, int)>)> GetResourceTaskInfoExporter(Project project)
    {
        var info = new Dictionary<string, (DateTime, int, List<(string, int)>)>();

        var tasks = project.TaskList;
        var resourcesTask = project.ResourceRelation;

        foreach (var task in tasks)
        {
            var taskTitle = task.Title;
            var startDate = task.StartTaskDate;
            var duration = task.DurationHours;

            info[taskTitle] = (startDate, duration, []);

            if (!resourcesTask.TryGetValue(taskTitle, out var resourceList)) continue;
            List<(string, int)> resourceInfo = resourceList
                .Select(r => (r.Item1.Name, r.Item2))
                .ToList();
            info[taskTitle] = (startDate, duration, resourceInfo);
        }

        return info;
    }

    private Project[] AllProjectThatUserAreLeader(UserDTO userDto)
    {
        var projectWhichHeIsLeader = _database.Projects.Query(context =>
                context.Project
                .Join(
                    context.ProjectUser.Where(pu => pu.Role == Roles.ProjectLeader)
                    .Join(
                        context.Users.Where(u => u.Email == userDto.Email),
                        pu => pu.UserId,
                        u => u.Id,
                        (pu, u) => pu
                        ),
                    p => p.Id,
                    pu => pu.ProjectId,
                    (p, pu) => p
                    )
                )
            .ToArray();
        return projectWhichHeIsLeader;
    }

    public ProjectDTO[] GetAllProjectThatUserAreLeader(UserDTO userDto)
    {
        var project = AllProjectThatUserAreLeader(userDto);
        return project.Select(ProjectDTO.ToEntity).ToArray();
    }

    private Project[] AllProjectThatUserAreManager(UserDTO userDto)
    {
        var projectWhichHeIsManager = _database.Projects.Query(context =>
                context.Project
                .Join(
                    context.ProjectUser.Where(pu => pu.Role == Roles.ProjectManager)
                    .Join(
                        context.Users.Where(u => u.Email == userDto.Email),
                        pu => pu.UserId,
                        u => u.Id,
                        (pu, u) => pu
                        ),
                    p => p.Id,
                    pu => pu.ProjectId,
                    (p, pu) => p
                    )
                )
            .ToArray();
        return projectWhichHeIsManager;
    }

    public ProjectDTO[] GetAllProjectThatUserAreManager(UserDTO userDto)
    {
        var project = AllProjectThatUserAreManager(userDto);
        return project.Select(ProjectDTO.ToEntity).ToArray();
    }

    private Project[] AllProjectThatUserAreMember(UserDTO userDto)
    {
        var projects = _database.Projects.Query(context =>
                context.Project
                    .Join(
                        context.ProjectUser.Where(pu => pu.UserId == userDto.Id),
                        p => p.Id,
                        pu => pu.ProjectId,
                        (p, pu) => p
                        )
                )
                .ToArray();
        return projects;
    }

    public ProjectDTO[] GetAllProjectThatUserAreMember(UserDTO userDto)
    {

        var project = AllProjectThatUserAreMember(userDto);
        return project.Select(ProjectDTO.ToEntity).ToArray();
    }

    public Error? SetProgrammedDateToStartTask(UserDTO applicant, ProjectDTO projectDto, TaskDTO task, DateTime dateTime)
    {
        var (projectTask, error) = GetProject(projectDto.Name);
        if (projectTask == null) return error;

        var realTask = _database.Tasks.FindBy(t => t.Id == task.Id);
        if (realTask == null)
            return new Error(Error.Types.KeyDoesNotExist, "No se encontro la tarea.");

        var haveTaskAssigned = HaveAssignedTask(projectTask, realTask, applicant.Id);
        if (!(haveTaskAssigned || IsManagerOfThisProject(applicant, projectDto)))
            return new Error(Error.Types.PermissionDenied, "No perteneces al equipo encargado de esta tarea");

        if (dateTime.CompareTo(TaskGraphService.GetDateTimeNow()) < 0)
            return new Error(Error.Types.OutOfRangeException, "La fecha de inicio programada no puede ser anterior a la fecha actual.");

        projectTask = ProjectConstruct.ConstructDeep(projectTask, _contextFactory.Generate());

        if (!new TaskGraphService(projectTask, _contextFactory).ValidTaskToStart(realTask))
            return new Error(Error.Types.StateCannotBeChanged, "La tarea no puede ser programada para empezar, termine primero las dependencias");

        var usedManyTimes = projectTask.ResourceRelation.ContainsKey(task.Title) && projectTask.ResourceRelation[task.Title].Any(resource =>
            resource.Item2 > resource.Item1.Quantity - ResourceService.GetNewInstance(_contextFactory)
                .GetResourceTotalUses(ResourceDTO.ToEntity(resource.Item1)));

        if (usedManyTimes)
            return new Error("ArgumentError", "La cantidad de recursos a asignar es mayor a la disponible");

        realTask.SetPlanedTaskDate(dateTime);

        _database.Tasks.Update(realTask);
        Reactivity.GetInstance().InvokeTaskGraph();

        return null;
    }

    private bool HaveAssignedTask(Project project, ProjectTask task, int userId)
    {
        return _database.TaskUsers.Exists(t => t.ProjectTaskId == task.Id && t.UserId == userId);
    }

    public List<(ProjectTask task, int quantity)> GetAllTasksUsingResource(Resource resource)
    {
        var context = _contextFactory.Generate();
        var taskQuantity = context.ProjectTask
            .Join(
                context.TaskResource.Where(rt => rt.ResourceId == resource.Id),
                t => t.Id,
                tr => tr.ProjectTaskId,
                (t, tr) => new { t, tr.Count }
            );

        var result = new List<(ProjectTask task, int quantity)>();

        foreach (var taskq in taskQuantity)
        {
            result.Add((taskq.t, taskq.Count));
        }

        return result;
    }
}
