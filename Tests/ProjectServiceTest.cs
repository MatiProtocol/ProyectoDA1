using System.Globalization;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Domain.HelpersDB;
using DomainConstruct;
using Microsoft.IdentityModel.Tokens;
using Services;
using Services.DTOs;

namespace Tests;

[TestClass]
public class ProjectServiceTest
{
    private ISqlContextFactory _contextFactory;
    private ProjectService _projectService = null!;
    private PureDatabase _db;
    private UserDTO _userDto = null!;
    private UserDTO _managerDto = null!;
    private UserDTO _leaderDto = null!;
    private UserDTO _systemManager = null!;
    private ProjectDTO _projectDto = null!;
    private Project _project = null!;
    private static int _i = 0;

    [TestInitialize]
    public void Initialize()
    {
        _i += 1;
        _contextFactory = new InMemorySqlContextFactory("ProjectServiceTest" + _i);
        
        ProjectService.Interval = 10 * 1000;
        ProjectService.SpanTime = 5;
        
        _projectService = ProjectService.GetNewInstance(_contextFactory);
        _db = PureDatabase.GetNewInstance(_contextFactory);
        
        _systemManager = new UserDTO
        {
            Name = "System",
            LastName = "Manager",
            Email = "test2@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.SystemManager
        };
        
        _userDto = new UserDTO
        {
            Name = "",
            LastName = "",
            Email = "test@test.com",
            BirthDate = "04/18/2002",
            Password = "123456Ac@",
            Role = Roles.ProjectMember
        };
        var user = _userDto.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        user.Role = Roles.ProjectMember;
        _db.Users.Add(user);
        _userDto = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _userDto.Email)!);


        var systemManager = _systemManager.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        _db.Users.Add(systemManager);
        _systemManager = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _systemManager.Email)!);

        _managerDto = new UserDTO
        {
            Name = "System",
            LastName = "Manager",
            Email = "test3@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.ProjectManager
        };

        var manager = _managerDto.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        _db.Users.Add(manager);
        _managerDto = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _managerDto.Email)!);

        _leaderDto = new UserDTO
        {
            Name = "System",
            LastName = "Manager",
            Email = "test4@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.ProjectLeader
        };

        var leader = _leaderDto.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        _db.Users.Add(leader);
        _leaderDto = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _leaderDto.Email)!);
        
        _project = new Project("Proyiuto", "Descricricri");
        _projectService.CreateProject(_systemManager, ProjectDTO.ToEntity(_project));

        ProjectService.Interval = 200;

        _project = _db.Projects.FindBy(p => p.Name.Equals("Proyiuto"))!;
        _projectDto = _projectService.GetProjectDto(_project.Name).Item1!;

    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void CreateProjectService_IsNotNull()
    {
        Assert.IsNotNull(_projectService);
    }
    
    [TestMethod]
    public void CreateProjectServiceWithoutFactory_IsNotNull()
    {
        _projectService = ProjectService.GetNewInstance(_contextFactory);
        Assert.IsNotNull(_projectService);
    }

    [TestMethod]
    public void CreateProjectServiceWithProjectMember_CratesWronglyReturnsError()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectMember;

        _db.Users.Update(user);
        
        var error = _projectService.CreateProject(_userDto, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void CreateProjectServiceWithNoneExistingUser_CratesWronglyReturnsError()
    {
        _userDto = new UserDTO
        {
            Name = "",
            LastName = "",
            Email = "pepepasdjfak@skafjaks.com",
            BirthDate = "04/18/2002",
            Password = "123456Ac@",
            Role = Roles.SystemManager
        };

        var error = _projectService.CreateProject(_userDto, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void CreateProjectService_CreatesCorrectlyReturnsNull()
    {
        _projectDto = new ProjectDTO
        {
            Name = "PEPEPP",
            Description = "PEPEPP es un cra"
        };
        var error = _projectService.CreateProject(_systemManager, _projectDto);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void CreateProject_WithRepeatedName()
    {
        var _projectDto = new ProjectDTO
        {
            Name = "Proyiuto",
            Description = "as"
        };
        
        var error = _projectService.CreateProject(_systemManager, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void CreateProjectServiceWithIncorrectProject_DoesNotCratesReturnsError()
    {
        var projectDtoWrong = new ProjectDTO();
        var error = _projectService.CreateProject(_systemManager, projectDtoWrong);
        Assert.IsInstanceOfType<Error>(error);
    }


    [TestMethod]
    public void DeleteProjectWithSystemManager_IsTrue()
    {
        Assert.IsTrue(_projectService.DeleteProject(_systemManager, _projectDto.Name));
    }

    [TestMethod]
    public void DeleteWrongProjectWithSystemManager_IsFalse()
    {
        Assert.IsFalse(_projectService.DeleteProject(_systemManager,
            "_projectDto.Name"));
    }

    [TestMethod]
    public void DeleteProjectWithProjectMember_IsFalse()
    {
        Assert.IsFalse(_projectService.DeleteProject(_userDto, _projectDto.Name));
    }

    [TestMethod]
    public void DeleteProjectWithProjectManager_IsFalse()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals("test@test.com"));
        user!.Role = Roles.ProjectManager;
        _db.Users.Update(user);

        byte[] bytePass = [0, 1, 1, 0, 2];

        _project.Team.Manager = new User("lalo",
            "genio",
            "Lalito@email.com",
            DateTime.Parse("10/10/2000",
                CultureInfo.CreateSpecificCulture("en-US")),
            bytePass);

        var error = _projectService.DeleteProject(_userDto, _projectDto.Name);
        Assert.IsFalse(error);
    }

    [TestMethod]
    public void DeleteProjectWithNoExisitingUser_IsFalse()
    {
        _userDto.Email = "PEPEP@JKAJSK.com";

        var error = _projectService.DeleteProject(_userDto, _projectDto.Name);
        Assert.IsFalse(error);
    }

    [TestMethod]
    public void DeleteProjectWithProjectManager_IsTrue()
    {
        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);
        Assert.IsTrue(_projectService.DeleteProject(_managerDto, _projectDto.Name));
    }

    [TestMethod]
    public void DeleteProjectWithExclusiveResources_IsTrue()
    {
        var resource = new Resource("res", "reves", "redes", 1)
        {
            Exclusive = true
        };
        var resourceService = ResourceService.GetNewInstance(_contextFactory);
        
        resourceService.CreateResource(_systemManager, ResourceDTO.ToEntity(resource), _projectDto, 1);
        
        var error = _projectService.DeleteProject(_systemManager, _projectDto.Name);
        Assert.IsTrue(error);
    }

    [TestMethod]
    public void GetProjectDTO_AreEqualToOriginalProjectDto()
    {
        var projectDto = _projectService.GetProjectDto(_project.Name).Item1;
        if (projectDto == null) return;
        var projectName = projectDto.Name;
        Assert.AreEqual(projectName, _projectDto.Name);
    }

    [TestMethod]
    public void GetProjectDTO_AreNotEqualToOriginalProjectDto()
    {
        var projectDto = _projectService.GetProjectDto(_project.Name).Item1;
        if (projectDto == null) return;
        var projectName = projectDto.Name;
        Assert.AreNotEqual(projectName, "_projectDto.Name");
    }

    [TestMethod]
    public void GetProjectDTOOfNoTrackedProject_ReturnsError()
    {
        var (_, error) = _projectService.GetProjectDto("Papelistri");
        Assert.IsInstanceOfType<Error>(error);

    }

    [TestMethod]
    public void ChangeProjectDtoName_ReturnsNull()
    {
        Assert.IsNull(_projectService.ChangeNameProject(_systemManager, _projectDto.Name, "Caruso Lombardi"));
    }

    [TestMethod]
    public void ChangeProjectDtoNameWithEmptyName_ReturnsNull()
    {
        Assert.IsInstanceOfType<Error>(_projectService.ChangeNameProject(_systemManager, "", "Caruso Lombardi"));
    }

    [TestMethod]
    public void ChangeProjectDtoNameWithEmptyNewName_ReturnsNull()
    {
        Assert.IsInstanceOfType<Error>(_projectService.ChangeNameProject(_systemManager, _projectDto.Name, ""));
    }

    [TestMethod]
    public void ChangeProjectDtoNameFromANotScopedProject_ReturnsError()
    {
        var error = _projectService.ChangeNameProject(_systemManager, "Name", "Caruso Lombardi");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjecDtoNameFromNotExistingUser_ReturnsError()
    {
        _systemManager.Email = "Pafjkjds@jks";
        var error = _projectService.ChangeNameProject(_systemManager, "Proyiuto", "Caruso Lombardi");

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjectDtoNameNotPermission_ReturnsError()
    {
        var error = _projectService.ChangeNameProject(_userDto, "Proyiuto", "Caruso Lombardi");

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjectNameNotPermission_ProjectManager_ReturnsError()
    {
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectManager);
        _userDto = userService.GetUserDto(_userDto.Email)!;

        var error = _projectService.ChangeNameProject(_userDto, "Proyiuto", "Caruso Lombardi");

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjectDtoNameOKPermission_ProjectManager_ReturnsNull()
    {
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectManager);
        _userDto = userService.GetUserDto(_userDto.Email)!;
        
        _projectService.AddManagerToProject(_systemManager, _userDto, _projectDto);

        var error = _projectService.ChangeNameProject(_userDto, _projectDto.Name, "Caruso Lombardi");

        Assert.IsNull(error);
    }

    [TestMethod]
    public void ChangeProjectDtoNameToDuplicateKeyReturnsError()
    {
        var error = _projectService.ChangeNameProject(_systemManager, "Proyiuto", "Proyiuto");

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjecDtoDesc_ReturnsNull()
    {
        Assert.IsNull(_projectService.ChangeDescProject(_systemManager, _projectDto.Name, "Caruso Lombardi"));
    }

    [TestMethod]
    public void ChangeInvalidProjectDtoDesc_ReturnsError()
    {
        var error = _projectService.ChangeDescProject(_systemManager, "Name", "Caruso Lombardi");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeInvalidProjectDtoToEmptyDesc_ReturnsError()
    {
        {
            var error = _projectService.ChangeDescProject(_systemManager, _projectDto.Name, "");
            Assert.IsInstanceOfType<Error>(error);
        }
    }

    [TestMethod]
    public void ChangeInvalidEmptyNameProjectDtoDesc_ReturnsError()
    {
        var error = _projectService.ChangeDescProject(_systemManager, "", "a");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void ChangeProjectDtoDescFromANotScopedProject_ReturnsError()
    {
        var error = _projectService.ChangeDescProject(_systemManager, "Name", "Caruso Lombardi");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddTaskToProject_ReturnsNull()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        var error = _projectService.AddTask(_systemManager, _projectDto, task);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void AddTaskToProjectWithRepeatedName_ReturnError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var error = _projectService.AddTask(_systemManager, _projectDto, tasks[0]);
        
        Assert.IsInstanceOfType<Error>(error);
    }


    [TestMethod]
    public void GetTasksWithNonTrackProject_returnNull()
    {
        _projectDto = new ProjectDTO()
        {
            Name = "Piripichiasdjfk",
        };
        Assert.AreEqual(0, _projectService.GetTasksByProject(_projectDto).Count);
    }

    [TestMethod]
    public void GetTaskWithTrackProject_returnTaskDTOList() =>
        Assert.IsInstanceOfType<List<TaskDTO>>(_projectService.GetTasksByProject(_projectDto));

    [TestMethod]
    public void AddTaskToProject_AssignCorrectly()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        _projectService.AddTask(_systemManager, _projectDto, task);
        
        var tasks = _projectService.GetTasksByProject(_projectDto);
        Assert.AreEqual("tarea", tasks?[0].Title);
    }

    [TestMethod]
    public void AddTaskToProjectProjectMember_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        var error = _projectService.AddTask(_userDto, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddEmptyTaskToProject_ReturnsError()
    {
        var task = new TaskDTO();

        var error = _projectService.AddTask(_systemManager, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddEmptyTaskToEmptyProject_ReturnsError()
    {
        var task = new TaskDTO();
        var projectDtoWrong = new ProjectDTO
        {
            Name = ""
        };
        var error = _projectService.AddTask(_systemManager, projectDtoWrong, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddNullTaskToProject_ReturnsError()
    {
        var task = new TaskDTO();
        var error = _projectService.AddTask(_systemManager, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddTaskToWrongProject_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        var projectDtoWrong = new ProjectDTO
        {
            Name = "novaresio"
        };
        var error = _projectService.AddTask(_systemManager, projectDtoWrong, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddTaskToProjectWithProjectManagerNotAdminOfThisProject_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var error = _projectService.AddTask(_managerDto, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);

    }

    [TestMethod]
    public void AddTaskToProjectWithProjectManagerAdminOfThisProject_NotReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);
        
        var error = _projectService.AddTask(_managerDto, _projectDto, task);
        Assert.IsNull(error);

    }


    [TestMethod]
    public void RemoveTaskFromProject_ReturnsNull()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);

        var tasks = _projectService.GetTasksByProject(_projectDto);

        var error = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }
    
    [TestMethod]
    public void RemoveTaskFromProjectWithResource_ReturnsNull()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        
        var resourceTest = new ResourceDTO
        {
            Name = "jennifer lopes",
            Description = "crack",
            Type = "volando",
            Quantity = 1
        };
        
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);

        var tasks = _projectService.GetTasksByProject(_projectDto);
        var resourceService = ResourceService.GetNewInstance(_contextFactory);
        resourceService.CreateResource(_systemManager, resourceTest, null, 1);
        var resAdder = new ResourceService.ResourceAdder
        {
            Quantity = 1,
            ResourceDto = resourceService.GetAllResourcesDto().Last(),
            TaskDto = tasks[0],
            ProjectDto = _projectDto
        };
        
        resourceService.AddResourceToTask(_systemManager, resAdder);
        var error = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }
    
    [TestMethod]
    public void RemoveProjectWithTaskAndResource_ReturnsNull()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        
        var resourceTest = new ResourceDTO
        {
            Name = "jennifer lopes",
            Description = "crack",
            Type = "volando",
            Quantity = 1
        };
        
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);

        var tasks = _projectService.GetTasksByProject(_projectDto);
        var resourceService = ResourceService.GetNewInstance(_contextFactory);
        resourceService.CreateResource(_systemManager, resourceTest, null, 1);
        var resAdder = new ResourceService.ResourceAdder
        {
            Quantity = 1,
            ResourceDto = resourceService.GetAllResourcesDto().Last(),
            TaskDto = tasks[0],
            ProjectDto = _projectDto
        };
        
        resourceService.AddResourceToTask(_systemManager, resAdder);
        var deleted = _projectService.DeleteProject(_systemManager, _projectDto.Name);
       
        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void RemoveTaskFromProjectProjectMember_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        var error = _projectService.RemoveTask(_userDto, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveEmptyTaskFromProject_ReturnsError()
    {
        var task = new TaskDTO();

        var error = _projectService.RemoveTask(_systemManager, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveEmptyTaskFromEmptyProject_ReturnsError()
    {
        var task = new TaskDTO();
        var projectDtoWrong = new ProjectDTO
        {
            Name = ""
        };

        var error = _projectService.RemoveTask(_systemManager, projectDtoWrong, task);

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveNullTaskFromProject_ReturnsError()
    {
        var task = new TaskDTO();

        var error = _projectService.RemoveTask(_systemManager, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveTaskWhichIsNotInProject_MustReturnError()
    {
        var task = new TaskDTO()
        {
            Title = "pep"
        };

        var error = _projectService.RemoveTask(_systemManager, _projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveTaskFromWrongProject_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        var projectDtoWrong = new ProjectDTO
        {
            Name = "novaresio"
        };

        var error = _projectService.RemoveTask(_systemManager, projectDtoWrong, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveTaskFromProjectWithProjectManagerNotAdminOfThisProject_ReturnsError()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        _systemManager.Role = Roles.ProjectManager;

        byte[] bytePass = [1, 3, 2, 1];
        _project.Team.Manager = new User("name", "jaime", "jaime@email.com",
            DateTime.Parse("10/10/2001", CultureInfo.CreateSpecificCulture("en-US")), bytePass);

        var projectDto = ProjectDTO.ToEntity(_project);

        var error = _projectService.RemoveTask(_systemManager, projectDto, task);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void RemoveTaskFromProjectWithProjectManagerAdminOfThisProject_ReturnsNull()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);
        _projectService.AddTask(_managerDto, _projectDto, task);
        
        var tasks=  _projectService.GetTasksByProject(_projectDto);

        var error = _projectService.RemoveTask(_managerDto, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void RemoveTaskFromProject_AndTheUserWhoWhereAssigned()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);

        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);

        var error = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void RemoveTaskFromProject_OtherCase()
    {
        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        var task1 = new TaskDTO();

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);
        _projectService.AddTask(_systemManager, _projectDto, task1);
        
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);
        _projectService.RemoveMemberFromTask(_systemManager, _projectDto, tasks[0], _systemManager);

        Error? error = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }
    
    [TestMethod]
    public void RemoveTaskFromProjectWithDependency_ReturnsNull()
    {
        var taskA = new TaskDTO
        {
            Title = "tarea A",
            Description = "la descripcion",
            DurationHour = 0
        };
        
        var taskB = new TaskDTO
        {
            Title = "tarea B",
            Description = "la descripcion",
            DurationHour = 0
        };

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, taskA);
        _projectService.AddTask(_systemManager, _projectDto, taskB);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        _projectService.AddDependencyToTask(_systemManager, _projectDto, new TaskGraphService.Edge
        {
            targetDto = tasks[0],
            depDto = tasks[1],
            type = TypeDependency.FS
        });

        var error = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsNull(error);
    }
    
    [TestMethod]
    public void RemoveProjectWithTasks_ReturnsNull()
    {
        var taskA = new TaskDTO
        {
            Title = "tarea A",
            Description = "la descripcion",
            DurationHour = 0
        };
        
        var taskB = new TaskDTO
        {
            Title = "tarea B",
            Description = "la descripcion",
            DurationHour = 0
        };

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, taskA);
        _projectService.AddTask(_systemManager, _projectDto, taskB);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddDependencyToTask(_systemManager, _projectDto, new TaskGraphService.Edge
        {
            targetDto = tasks[0],
            depDto = tasks[1],
            type = TypeDependency.FS
        });

        var result = _projectService.DeleteProject(_systemManager, _projectDto.Name);
        Assert.IsTrue(result);
    }
    

    [TestMethod]
    public void AddManagerToProject_ReturnsNull()
    {
        var error = _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void AddManagerToProjectApplicantIsMember_ReturnsError()
    {
        var error = _projectService.AddManagerToProject(_userDto, _systemManager, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddManagerToNotTrackProject_ReturnsError()
    {
        _projectDto.Name = "Piringundin";
        var error = _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddManagerToProjectUserIsMember_ReturnsNull()
    {
        var error = _projectService.AddManagerToProject(_systemManager, _userDto, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddManagerToProjectNotTrackedUser_ReturnsError()
    {
        _systemManager.Email = "pepe@ppep.com";
        var error = _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddManagerToProject_NewManagerDoesNotHavePermission_ReturnsNull()
    {
        _userDto.Email = "Peojkasdf@jskd";
        var error = _projectService.AddManagerToProject(_systemManager, _userDto, _projectDto);
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void IsManagerOfTheIsProjectWithNonTrackProject_returnFalse()
    {
        var _projectDto = new ProjectDTO
        {
            Name = "Piringundin",
            Description = "bot"
        };

        var manageTheProject = _projectService.IsManagerOfThisProject(_userDto, _projectDto);

        Assert.IsFalse(manageTheProject);
    }

    [TestMethod]
    public void IsManagerOfThisProjectWithWrongManager_returnFalse()
    {
        var user = new User(_systemManager.Name, _systemManager.LastName, _systemManager.Email,
            DateTime.Parse(_systemManager.BirthDate, CultureInfo.CreateSpecificCulture("en-US")), [1, 2, 3]);

        _project.Team.Manager = user;

        var manageTheProject = _projectService.IsManagerOfThisProject(_userDto, _projectDto);

        Assert.IsFalse(manageTheProject);
    }

    [TestMethod]
    public void IsManagerOfThisProjectWithCorrectlyManager_returnTrue()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var manageTheProject = _projectService.IsManagerOfThisProject(_systemManager, _projectDto);

        Assert.IsTrue(manageTheProject);
    }

    [TestMethod]
    public void AddMemberToProjectWithNonTrackProject_ReturnError()
    {
        _projectDto.Name = "Piringundin";
        Assert.IsInstanceOfType<Error>(_projectService.AddMemberToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void AddMemberToProjectWithOutManagerButIsSystemAdmin_ReturnNull()
    {
        Assert.IsNull(_projectService.AddMemberToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void AddMemberToProjectWithOutManager_ApplicantIsManager_returnError()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_systemManager.Email));
        user!.Role = Roles.ProjectManager;
        _db.Users.Update(user);
        
        Assert.IsInstanceOfType<Error>(_projectService.AddMemberToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void AddMemberToProjectWithAlreadyMemberUser_ReturnError()
    {

        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        Assert.IsInstanceOfType<Error>(_projectService.AddMemberToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void IsMemberOfProjectWithValidMember_ReturnTrue()
    {
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        Assert.IsTrue(_projectService.IsMemberOfThisProject(_userDto, _projectDto));
    }

    [TestMethod]
    public void IsMemberOfProjectWithInvalidMember_ReturnFalse()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        Assert.IsFalse(_projectService.IsMemberOfThisProject(_userDto, _projectDto));
    }

    [TestMethod]
    public void IsMemberOfProjectWithNonTrackProject_ReturnFalse()
    {
        _projectDto.Name = "Piringundin";
        Assert.IsFalse(_projectService.IsMemberOfThisProject(_userDto, _projectDto));
    }

    [TestMethod]
    public void GetAllProjects_isNotNull() => Assert.IsNotNull(_projectService.GetAllProjectsDto());

    [TestMethod]
    public void GetAllProjects_ReturnsArrayOfProjects()
    {
        _projectDto = new ProjectDTO
        {
            Name = "Piringundin",
            Description = "bot"
        };
        _projectService.CreateProject(_systemManager, _projectDto);
        
        var projects = _projectService.GetAllProjectsDto();
        Assert.IsTrue(projects is [{ Name: "Proyiuto" }, { Name: "Piringundin" }]);
    }

    [TestMethod]
    public void GetProjectsByUser_isNotNull() => Assert.IsNotNull(_projectService.GetProjectsByUser(_userDto));

    [TestMethod]
    public void GetProjectsByUser_ReturnsArrayOfProjects()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var projects = _projectService.GetProjectsByUser(_systemManager);
        Assert.IsTrue(projects.Length == 1);
    }
    
    [TestMethod]
    public void GetProjectsByUserWithNonTrackUser_ReturnsArrayOfProjects()
    {
        _userDto = new UserDTO
        {
            Name = "",
            LastName = "",
            Email = "testasdas@test.com",
            BirthDate = "04/18/2002",
            Password = "123456Ac@",
            Role = Roles.ProjectMember
        };

        var projects = _projectService.GetProjectsByUser(_userDto);
        Assert.IsTrue(projects.Length == 0);
    }

    [TestMethod]
    public void RemoveManagerFromProjectApplicantIsMember_ReturnsError()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_systemManager.Email));
        user!.Role = Roles.ProjectMember;
        _db.Users.Update(user);

        var result = _projectService.RemoveManagerFromProject(_systemManager, _projectDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveManagerFromProjectProjectNotExist_ReturnError()
    {
        _projectDto = new ProjectDTO
        {
            Name = "Piringundin",
            Description = "fiumba"
        };

        var result = _projectService.RemoveManagerFromProject(_systemManager, _projectDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveManagerFromProjectApplicantIsManagerNotOfProject_ReturnsError()
    {
        
        var userService = UserService.GetNewInstance(_contextFactory);
        _userDto.Role = Roles.ProjectManager;
        userService.CreateUser(_systemManager, _userDto);

        _systemManager.Role = Roles.ProjectManager;
        
        var result = _projectService.RemoveManagerFromProject(_userDto, _projectDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void RemoveManagerFromProject_RemoveCorrectly()
    {

        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectManager;
        _db.Users.Update(user);
        
        _projectService.AddManagerToProject(_systemManager, _userDto, _projectDto);
        
        var error = _projectService.RemoveManagerFromProject(_userDto, _projectDto);
        
        Assert.IsNull(error);
    }

    [TestMethod]
    public void RemoveMemberFromProjectMemberIsTheManager_ReturnsError()
    {

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var result = _projectService.RemoveMemberFromProject(_systemManager, _systemManager, _projectDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveMemberFromProjectApplicantIsMember_ReturnsError()
    {
        
        _systemManager.Role = Roles.ProjectMember;
        
        var result = _projectService.RemoveMemberFromProject(_systemManager, _userDto, _projectDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void RemoveMemberFromProjectProjectNotExist_ReturnError()
    {
        
        _projectDto.Name = "Piringundin";
        
        var result = _projectService.RemoveMemberFromProject(_systemManager, _userDto, _projectDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveMemberFromProjectNotIsAMemberOfThisProject_ReturnsError()
    {
        var result = _projectService.RemoveMemberFromProject(_systemManager, _userDto, _projectDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void RemoveMemberFromProject_RemoveCorrectly()
    {
        
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _userDto);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        
        var result = _projectService.RemoveMemberFromProject(_systemManager, _userDto, _projectDto);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetTaskCanStartWithNonTrackProject_ReturnError()
    {
        
        _projectDto.Name = "Piringundin";
        
        var result = _projectService.GetTaskCanStart(_projectDto).Item2;
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void GetTaskCanStartWithTrackProject_ReturnArrayOfTaskDTO()
    {
        var result = _projectService.GetTaskCanStart(_projectDto).Item1;
        
        Assert.IsInstanceOfType<TaskDTO[]>(result);
    }

    [TestMethod]
    public void GetTaskCanStartWithTrackProject_ReturnArrayOfTaskDTOWithCorrectData()
    {
        
        var task = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea"
        };
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);
        
        var result = _projectService.GetTaskCanStart(_projectDto).Item1;
        
        Assert.IsTrue(result != null && result[0].Title == "TaskA");

    }

    [TestMethod]
    public void AssignedMemberToProject_FirstTime()
    {
        
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _userDto);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        
        var result = _projectService.RemoveMemberFromProject(_systemManager, _userDto, _projectDto);
        
        Assert.IsNull(result);
    }


    [TestMethod]
    public void AssignUserToTask_EveryDataValid()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void AssignUserToTask_ApplicantHasNotEnoughPermission()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.AddMemberToTask(_userDto, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AssignUserToTask_TargetDoesNotBelongInTeam()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AssignUserToTask_TaskDoesNotExist()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        
        var result = _projectService.AddMemberToTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1), _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UnassignUserToTask_ValidData()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1));

        var tasks = _projectService.GetTasksByProject(_projectDto);

        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        var result = _projectService.RemoveMemberFromTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnassignUserToTask_NotEnoughPermission()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));

        _projectService.AddMemberToTask(_systemManager, _projectDto, TaskDTO.ToEntity(task), _userDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.RemoveMemberFromTask(_userDto, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UnassignUserToTask_TargetDoesNotBelongInProject()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));

        _projectService.AddMemberToTask(_systemManager, _projectDto, TaskDTO.ToEntity(task), _userDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.RemoveMemberFromTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UnassignUserToTask_TaskDoesNotExist()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.RemoveMemberFromTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UnassignUserToTask_UserIsNotAssaingToTask()
    {
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.RemoveMemberFromTask(_systemManager, _projectDto, tasks[0], _userDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskTitle_ChangeTitleCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Title = "TaskB";
        
        _ = _projectService.ChangeTaskTitle(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)![0];
        
        Assert.IsTrue(result.Title == "TaskB");
    }

    [TestMethod]
    public void ChangeTaskTitleWithUserAssigned_ChangeTitleCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Title = "TaskB";

        
        _ = _projectService.ChangeTaskTitle(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(result is { Title: "TaskB" });
    }

    [TestMethod]
    public void ChangeTaskTitleWithLowRoleSysManager_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Title = "TaskB";

        
        var result = _projectService.ChangeTaskTitle(_userDto, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskTitleWithOutTasks_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Title = "TaskB";
        
        var result = _projectService.ChangeTaskTitle(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskTitleWithEmptyTitle_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        
        var result = _projectService.ChangeTaskTitle(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskDescription_ChangeDescriptionCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Description = "Segunda Tarea";

        
        _ = _projectService.ChangeTaskDescription(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(result is { Description: "Segunda Tarea" });
    }

    [TestMethod]
    public void ChangeTaskDescriptionWithLowRoleSysManager_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Description = "Segunda Tarea";

        
        var result = _projectService.ChangeTaskDescription(_userDto, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskDescriptionWithOutTasks_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.Description = "Segunda Tarea";
        
        var result = _projectService.ChangeTaskDescription(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskDuration_ChangeDurationCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        
        taskDto = _projectService.GetTasksByProject(_projectDto)![0];

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.DurationHour = 2;

        
        _ = _projectService.ChangeTaskDuration(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(result is { DurationHour: 2 });
    }

    [TestMethod]
    public void ChangeTaskDurationWithLowRoleSysManager_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        taskDto = _projectService.GetTasksByProject(_projectDto)![0];

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.DurationHour = 2;
        
        var result = _projectService.ChangeTaskDuration(_userDto, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskDurationWithOutTasks_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.DurationHour = 2;
        
        var result = _projectService.ChangeTaskDuration(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateToStarted_ChangeStateCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        taskDto = _projectService.GetTasksByProject(_projectDto)![0];

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;
        changeTaskHelper.NewInfoTaskDto.StartTaskDate = DateTime.Now;
        _projectService.AddMemberToTask(_systemManager, _projectDto, taskDto, _systemManager);
        
        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(result is { State: TaskState.Started });
    }

    [TestMethod]
    public void ChangeTaskStateToStarted_ButResourceIsBeingUsed()
    {
        // Arrange
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var taskDto1 = new TaskDTO
        {
            Title = "TaskB",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto1);

        var tasks = _projectService.GetTasksByProject(_projectDto);
        var res = new ResourceDTO()
        {
            Name = "TaskA",
            Type = "pe",
            Description = "Primera tarea",
            Quantity = 1,
        };
        ResourceService.GetNewInstance(_contextFactory).CreateResource(_systemManager, res, null, 1);

        var adder = new ResourceService.ResourceAdder()
        {
            ResourceDto = res,
            TaskDto = tasks[1],
            ProjectDto = _projectDto,
            Quantity = 1,
        };

        ResourceService.GetNewInstance(_contextFactory).AddResourceToTask(_systemManager, adder);

        adder.TaskDto = tasks[0];

        ResourceService.GetNewInstance(_contextFactory).AddResourceToTask(_systemManager, adder);
        
        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;

        // Act
        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);

        changeTaskHelper.OriginalTaskDto = tasks[1];
        var error = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);

        // Assert
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void ChangeTaskStateWithLowRoleSysManager_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        taskDto = _projectService.GetTasksByProject(_projectDto)![0];

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;
        
        var result = _projectService.ChangeTaskState(_userDto, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateWithOutTasks_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Finished;
        
        var result = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateToLowerState_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            State = TaskState.Finished
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;

        
        var result = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateWithNonTrackProject_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "Taskj",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectDto.Name = "psdjkfjdaskiripichiaaa";

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;

        
        var result = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateWithPendingDependence_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskJ",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        var taskDtoB = new TaskDTO
        {
            Title = "TaskK",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };

        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        _projectService.AddTask(_systemManager, _projectDto, taskDtoB);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var edge = new TaskGraphService.Edge
        {
            targetDto = tasks[0],
            depDto = tasks[1],
            type = TypeDependency.FS
        };

        _projectService.AddDependencyToTask(_systemManager, _projectDto, edge);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);

        
        var result = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeTaskStateWithNonTrackTask_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;

        
        var result = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void ChangeTaskStateToFinish_ChangeStateCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;
        changeTaskHelper.NewInfoTaskDto.StartTaskDate = DateTime.Now;

        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);
        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Finished;
        changeTaskHelper.NewInfoTaskDto.EndTaskDate = DateTime.Now;


        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(result is { State: TaskState.Finished });
    }

    [TestMethod]
    public void ChangeTaskStateWithUsedResource_ReturnArgumentError()
    {
        ProjectService.Interval = 100 * 60 * 1000;
        ProjectService.SpanTime = 30;
        _projectService.GetUpdateProjectState(_projectDto, false);
        var resource = new ResourceDTO()
        {
            Name = "Hola93203", 
            Type = "Masa9129", 
            Description = "Interminable", 
            Quantity = 1,
        };
        var resS = ResourceService.GetNewInstance(_contextFactory);
        resS.CreateResource(_systemManager, resource, null, 1);
        var resourceDto = resS.GetAllResourcesDto().Last();

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
            
        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };

        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        
        var taskDto2 = new TaskDTO
        {
            Title = "TaskB",
            Description = "Segunda tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto2);
        

        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var resData =new ResourceService.ResourceAdder()
        {
            ResourceDto = resourceDto,
            Quantity = 1,
            ProjectDto = _projectDto,
            TaskDto = tasks[0] 
        };
        
        resS.AddResourceToTask(_systemManager, resData);
        resData.TaskDto = tasks[1];
        resS.AddResourceToTask(_systemManager, resData);
        
        var taskDtoNewInfo = new TaskDTO();
        taskDtoNewInfo.State = TaskState.Started;
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;

        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);
        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);

        changeTaskHelper.OriginalTaskDto = tasks[1];
        // Act

        var error = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];

        // Assert
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetProgrammedDateToStartToTaskWithUsedResource_ReturnArgumentError()
    {
        ProjectService.Interval = 100 * 60 * 1000;
        ProjectService.SpanTime = 30;
        
        _projectService.GetUpdateProjectState(_projectDto, false);
        var resource = new ResourceDTO()
        {
            Name = "Hola93203", 
            Type = "Masa9129", 
            Description = "Interminable", 
            Quantity = 1,
        };
        var resS = ResourceService.GetNewInstance(_contextFactory);
        resS.CreateResource(_systemManager, resource, null, 1);
        var resourceDto = resS.GetAllResourcesDto().Last();

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };

        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        
        var taskDto2 = new TaskDTO
        {
            Title = "TaskB",
            Description = "Segunda tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto2);
        
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var resData =new ResourceService.ResourceAdder()
        {
            ResourceDto = resourceDto,
            Quantity = 1,
            ProjectDto = _projectDto,
            TaskDto = tasks[0] 
        };
        resS.AddResourceToTask(_systemManager, resData);
        resData.TaskDto = tasks[1];
        resS.AddResourceToTask(_systemManager, resData);

        var taskDtoNewInfo = new TaskDTO();
        taskDtoNewInfo.State = TaskState.Started;

        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _systemManager);
        _ = _projectService.SetProgrammedDateToStartTask(_systemManager, _projectDto, tasks[0], DateTime.Today.AddDays(1)); ;

        // Act

        var error = _projectService.SetProgrammedDateToStartTask(_systemManager, _projectDto, tasks[1], DateTime.Today.AddDays(1));
        var result = _projectService.GetTasksByProject(_projectDto)?[0];

        // Assert
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddDepToTask_AllValidData()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));

        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge
                {
                    targetDto = tasks[0],
                    depDto = tasks[1],
                    type = TypeDependency.FS
                });
        
        _project = _projectService.GetProject(_project.Name).Item1!;

        Assert.IsNull(result);
    }

    [TestMethod]
    public void AddDepToTaskManyTimes_ReturnError()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1));

        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
            new TaskGraphService.Edge()
            {
                targetDto = tasks[0],
                depDto = tasks[1],
                type = TypeDependency.FS
            });
        result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
            new TaskGraphService.Edge()
            {
                targetDto = tasks[0],
                depDto = tasks[1],
                type = TypeDependency.FS
            });

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AddDepToTaskWithFinishedTask_ReturnError()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        task.SetEndTaskDate(DateTime.Now);

        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1));

        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
            new TaskGraphService.Edge()
            {
                targetDto = tasks[0],
                depDto = tasks[1],
                type = TypeDependency.FS
            });

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AddDepToTask_NotEnoughPermission()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.AddDependencyToTask(_userDto, _projectDto,
                new TaskGraphService.Edge()
                {
                    targetDto = tasks[0],
                    depDto = tasks[1],
                    type = TypeDependency.FS
                });
        
        _project = _projectService.GetProject(_project.Name).Item1!;

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AddDepToTask_TargetIsNotInList()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge()
                {
                    targetDto = TaskDTO.ToEntity(task),
                    depDto = tasks[0],
                    type = TypeDependency.FS
                });

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AddDepToTask_DepIsNotInList()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge()
                {
                    targetDto = tasks[0],
                    depDto = TaskDTO.ToEntity(task1),
                    type = TypeDependency.FS
                });

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void AddDepToTask_Cycle()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));

        _projectDto = _projectService.GetProjectDto(_project.Name).Item1!;
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge
                {
                    targetDto = tasks[0],
                    depDto = tasks[1],
                    type = TypeDependency.FS
                });
        
        var result2 = _projectService.AddDependencyToTask(_systemManager, _projectDto,
            new TaskGraphService.Edge
            {
                targetDto = tasks[1],
                depDto = tasks[0],
                type = TypeDependency.FS
            });

        Assert.IsInstanceOfType<Error>(result2);
    }


    [TestMethod]
    public void RemoveDepFromTask_ValidData()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));

        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1));
        
        var tasks = _projectService.GetTasksByProject(_projectDto);

        _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge()
                {
                    targetDto = tasks[0],
                    depDto = tasks[1],
                    type = TypeDependency.FS
                });

        var result = _projectService.RemoveDependencyFromTask(_systemManager, _projectDto, tasks[0], tasks[1]);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void RemoveDepFromTask_EdgeDoesNotExist()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var result = _projectService.RemoveDependencyFromTask(_systemManager, _projectDto, tasks[0], tasks[1]);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveDepFromTask_NotEnoughPermission()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var result = _projectService.RemoveDependencyFromTask(_userDto, _projectDto, tasks[0], tasks[1]);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveDepFromTask_TargetIsNotInList()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.RemoveDependencyFromTask(_systemManager, _projectDto, TaskDTO.ToEntity(task), tasks[0]);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void RemoveDepFromTask_DepIsNotInList()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.RemoveDependencyFromTask(_systemManager, _projectDto, tasks[0], TaskDTO.ToEntity(task1));

        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void GetTaskDependencies_ValidData()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddDependencyToTask(_systemManager, _projectDto, new TaskGraphService.Edge
            {
                targetDto = tasks[0],
                depDto = tasks[1],
                type = TypeDependency.FS
            });

        var result = _projectService.GetTaskDependencies(_projectDto, TaskDTO.ToEntity(task));

        Assert.AreEqual(TaskDTO.ToEntity(task1).Title, result![0].task.Title);
    }

    [TestMethod]
    public void GetTaskDependencies_InvalidProject()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var result = _projectService.GetTaskDependencies(ProjectDTO.ToEntity(new Project("pep", "asdkf")), TaskDTO.ToEntity(task));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetTaskDependencies_InvalidTask()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);

        var result = _projectService.GetTaskDependencies(_projectDto, TaskDTO.ToEntity(task));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TaskDependenciesVerifyEdge_ValidData()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task1));

        var tasks = _projectService.GetTasksByProject(_projectDto);

        var result = _projectService.TaskDependencyVerifyEdge(_projectDto, tasks[0], tasks[1]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TaskDependenciesVerify_NoValidEdge()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.TaskDependencyVerifyEdge(_projectDto, tasks[0], tasks[0]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TaskDependenciesVerifyEdge_InvalidTarget()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var result = _projectService.TaskDependencyVerifyEdge(_projectDto, TaskDTO.ToEntity(task1), tasks[0]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TaskDependencyVerifyEdge_InvalidDep()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var result = _projectService.TaskDependencyVerifyEdge(_projectDto, tasks[0], TaskDTO.ToEntity(task1));

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TaskDependencyVerifyEdge_InvalidProject()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);

        _projectDto.Name = "jkasdfjkasjdfkasfdkal";

        var result = _projectService.TaskDependencyVerifyEdge(_projectDto, tasks[1], tasks[0]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetCriticalPath_InvalidProject()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        
        _projectDto.Name = "jkasdfjkasjdfkasfdkal";

        var result = _projectService.GetCriticalPath(_projectDto);

        Assert.AreEqual(([], 0), result);
    }

    [TestMethod]
    public void GetCriticalPath_ValidProject()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 11);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.GetCriticalPath(_projectDto);

        Assert.AreEqual((tasks[1].Title, 11 / 8 * 24 + 11 % 8), (result.path[0].Title, result.duration));
    }

    [TestMethod]
    public void CalculateEstimation_InvalidProject()
    {
        _projectDto.Name = "jkasdfjkasjdfkasfdkal";

        var result = _projectService.CalculateEstimation(_projectDto);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CalculateEstimation_ValidProject()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 11);
        
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));

        var result = _projectService.CalculateEstimation(_projectDto);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetProjectState_ValidProject()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        var result = _projectService.GetUpdateProjectState(_projectDto);

        Assert.AreEqual(TaskState.Pending, result);
    }

    [TestMethod]
    public void GetProjectState_OneTaskStarted()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        task.State = TaskState.Started;

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        var result = _projectService.GetUpdateProjectState(_projectDto);

        Assert.AreEqual(TaskState.Started, result);
    }

    [TestMethod]
    public void GetProjectState_AllTaskFinished()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10)
        {
            State = TaskState.Finished
        };

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        
        var result = _projectService.GetUpdateProjectState(_projectDto);

        Assert.AreEqual(TaskState.Finished, result);
    }

    [TestMethod]
    public void GetProjectState_InvalidProject()
    {
        _projectDto.Name = "asdjkfas";

        var result = _projectService.GetUpdateProjectState(_projectDto);

        Assert.AreEqual(TaskState.Pending, result);
    }

    [TestMethod]
    public void GetProjectState_WhenPendingSetStartToday()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        _ = _projectService.GetUpdateProjectState(_projectDto);

        (_project, _) = _projectService.GetProject(_project.Name); 
        Assert.IsTrue(Math.Abs((DateTime.Now - _project!.StartDate).TotalSeconds) <= 5);
    }

    [TestMethod]
    public void GetProjectState_WhenPlanedTaskIsInPast()
    {
        var task = new ProjectTask("asdjf", "jasdkf", 10)
        {
            State = TaskState.Finished
        };

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));

        _ = _projectService.GetUpdateProjectState(_projectDto);
        _ = _projectService.GetUpdateProjectState(_projectDto);
        
        Assert.IsTrue(Math.Abs((DateTime.Now - _project.EndDate).Minutes) <= 1);
    }

    [TestMethod]
    public void IsLeaderOfThisProject_ReturnTrue()
    {
        _projectService.SetLeaderToProject(_systemManager, _leaderDto, _projectDto);
        
        var result = _projectService.IsLeaderOfThisProject(_leaderDto, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsLeaderOfThisProject_ReturnFalse()
    {
        var result = _projectService.IsLeaderOfThisProject(_userDto, _projectDto);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsLeaderOfThisProjectWithNonTrackProject_ReturnFalse()
    {
        var newProjectDto = new ProjectDTO
        {
            Name = "asdjkfas"
        };

        var result = _projectService.IsLeaderOfThisProject(_userDto, newProjectDto);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SetLeaderToProject_SetLeaderCorrectly()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectLeader;
        _db.Users.Update(user);

        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        var result = _projectService.IsLeaderOfThisProject(_userDto, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SetLeaderToProjectWithLowerRole_ReturnError()
    {
        Assert.IsInstanceOfType<Error>(_projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void SetLeaderToProjectWithLowerRoleApplicant_ReturnError()
    {
        _systemManager.Role = Roles.ProjectLeader;

        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        Assert.IsInstanceOfType<Error>(_projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto));
    }

    [TestMethod]
    public void SetLeaderToProjectWithNonTrackApplicant_ReturnError()
    {
        _systemManager.Email = "fiumba@email.com";
        _systemManager.Role = Roles.ProjectManager;

        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
    }

    [TestMethod]
    public void SetLeaderToProjectWithNonTrackUser_ReturnError()
    {
        _userDto.Email = "fiumba@email.com";
        _userDto.Role = Roles.ProjectManager;

        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
    }
    
    [TestMethod]
    public void SetLeaderWithAnotherUserLeadTheProject_SetLeaderCorrectly()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectLeader;
        _db.Users.Update(user);

        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _leaderDto, _projectDto);

        var result = _projectService.IsLeaderOfThisProject(_leaderDto, _projectDto);
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void SetLeaderPromoteUserToLeader_SetLeaderCorrectly()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectLeader;
        _db.Users.Update(user);

        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        var result = _projectService.IsLeaderOfThisProject(_userDto, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void RemoveLeaderToProject_RemoveLeaderCorrectly()
    {
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectLeader);
        
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
        _projectService.RemoveLeaderToProject(_systemManager, _projectDto);

        var result = _projectService.IsMemberOfThisProject(_userDto, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void RemoveLeaderToProjectWithNonTrackApplicant_ReturnError()
    {
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        _systemManager.Email = "Pedrito@fiumba.com";

        Assert.IsInstanceOfType<Error>(_projectService.RemoveLeaderToProject(_systemManager, _projectDto));
    }
    
    [TestMethod]
    public void RemoveLeaderToProjectWithNonTrackUser_ReturnError()
    {
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        _systemManager = new UserDTO
        {
            Name = "System",
            LastName = "Manager",
            Email = "test232@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.SystemManager
        };
        Assert.IsInstanceOfType<Error>(_projectService.RemoveLeaderToProject(_systemManager, _projectDto));
    }

    [TestMethod]
    public void IsPartOfTheProjectWhitManager_returnTrue()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        var manageTheProject = _projectService.IsPartOfTheProject(_systemManager, _projectDto);

        Assert.IsTrue(manageTheProject);
    }

    [TestMethod]
    public void IsPartOfTheProjectWhitMember_returnTrue()
    {

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var result = _projectService.IsPartOfTheProject(_userDto, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPartOfTheProjectWhitLeader_returnTrue()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        var result = _projectService.IsPartOfTheProject(_systemManager, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsPartOfTheProjectWhitUserOutOfProject_returnFalse()
    {
        _userDto.Role = Roles.ProjectLeader;
        _userDto.Email = "fiumba@.com";
        _db.Projects.FindBy(p => p.Name.Equals(_project.Name))!.Team.Leader = _userDto.FromEntity([1, 2, 3]).Item1;

        var result = _projectService.IsPartOfTheProject(_userDto, _projectDto);
        Assert.IsFalse(result);
    }


    [TestMethod]
    public void IsManagerOrLeaderWhitManager_returnTrue()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        var manageTheProject = _projectService.IsManagerOrLeader(_systemManager, _projectDto);

        Assert.IsTrue(manageTheProject);
    }

    [TestMethod]
    public void IsManagerOrLeaderWhitLeader_returnTrue()
    {
        _projectService.SetLeaderToProject(_systemManager, _systemManager, _projectDto);
        var result = _projectService.IsManagerOrLeader(_systemManager, _projectDto);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsManagerOrLeaderWhitUserOutOfProject_returnFalse()
    {
        _userDto.Role = Roles.ProjectLeader;
        _userDto.Email = "fiumba@.com";
        _db.Projects.FindBy(p => p.Name.Equals(_project.Name))!.Team.Leader = _userDto.FromEntity([1, 2, 3]).Item1;

        var result = _projectService.IsManagerOrLeader(_userDto, _projectDto);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetUsersCanBeLeader_ReturnsUsersWithProjectLeaderOrHigherRoleExcludingManager()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectLeader;
        _db.Users.Update(user);
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var result = projectService.GetUsersCanBeLeader(_projectDto);

        Assert.IsTrue(result[0].Email == _userDto.Email);
    }


    [TestMethod]
    public void GetUsersCanBeLeaderWhitNonTrackProject_ReturnEmpty()
    {
        var user = _db.Users.FindBy(u => u.Email.Equals(_userDto.Email));
        user!.Role = Roles.ProjectLeader;
        _db.Users.Update(user);
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var projectService = ProjectService.GetNewInstance(_contextFactory);
        
        _projectDto = new ProjectDTO()
        {
            Name = "Piripichoajsdkf",
        };
        var result = projectService.GetUsersCanBeLeader(_projectDto);

        Assert.IsTrue(result.Length == 0);
    }

    [TestMethod]
    public void ExportProjectWithNonTrackLeader_ReturnError()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        task.SetStartTaskDate(DateTime.Today);
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
    
        var error = _projectService.ExportProject(_userDto, true).Item2;
    
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void ExportProjectUserLowerRole_ReturnError()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        task.SetStartTaskDate(DateTime.Today);
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        _project = _projectService.GetProject(_projectDto.Name).Item1!;
        _project = ProjectConstruct.ConstructDeep(_project, _contextFactory.Generate());
        task = _project.TaskList[0];
    
        var resource = new ResourceDTO
        {
            Name = "Recurso",
            Description = "keloke",
            Type = "fiumbaResource",
            Quantity = 1
        };
            
        var resourceService = ResourceService.GetNewInstance(_contextFactory);
        resourceService.CreateResource(_systemManager, resource, null, 1);
    
        var resourceAdder = new ResourceService.ResourceAdder()
        {
            ProjectDto = _projectService.GetProjectDto(_project.Name).Item1!,
            ResourceDto = ResourceDTO.ToEntity(_db.Resources.FindBy(r => r.Name == resource.Name)!),
            TaskDto = tasks[0],
            Quantity = 1
        };
            
        resourceService.AddResourceToTask(_systemManager, resourceAdder);
    
        _userDto.Role = Roles.ProjectMember;
        var error = _projectService.ExportProject(_userDto, true).Item2;
    
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void ExportProjectLeaderWithoutProject_ReturnError()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        task.SetStartTaskDate(DateTime.Today);
        _project.TaskList.Add(task);
    
        var resource = new Resource("Recurso", "keloke", "fiumbaResource", 1);
        _project.ResourceRelation[task.Title] = [(resource, 3)];
    
        var error = _projectService.ExportProject(_userDto, true).Item2;
    
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void ExportProjectAsJson_ShouldCreateFileWithExpectedContent()
    {
        
        var task = new ProjectTask("Tarea", "Descripción", 5);
        
        _projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(task));
        task = _projectService.GetTasksByProject(_projectDto)![0].FromEntity().Item1;

        var resource = new ResourceDTO
        {
            Name = "Recurso",
            Description = "keloke",
            Type = "fiumbaResource",
            Quantity = 1
        };
        
        var resourceService = ResourceService.GetNewInstance(_contextFactory);
        

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectLeader);

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
        
        resourceService.CreateResource(_systemManager, resource, null, 1);
        
        var resourceAdder = new ResourceService.ResourceAdder
        {
            ProjectDto = _projectService.GetProjectDto(_project.Name).Item1!,
            ResourceDto = ResourceDTO.ToEntity(_db.Resources.FindBy(r => r.Name == resource.Name)!),
            TaskDto = TaskDTO.ToEntity(task!),
            Quantity = 1
        };
        
        resourceService.AddResourceToTask(_systemManager, resourceAdder);

        var (downloader , error) = _projectService.ExportProject(_userDto, true);

        var content = downloader!.Value.Content;
        var mimeType = downloader.Value.MimeType;
        var fileName = downloader.Value.FileName;
        
        Assert.IsTrue(error is null && 
                      mimeType == "application/json" &&
                      fileName == "projects.json" &&
                      content.Contains("Proyiuto") &&
                      content.Contains("Tarea") &&
                      content.Contains("Recurso") &&
                      content.Contains(DateTime.Today.ToString("MM/dd/yyyy")));
    }
    
    [TestMethod]
    public void ExportProjectAsCSV_ShouldCreateFileWithExpectedContent()
    {
        
        var task = new ProjectTask("Tarea", "Descripción", 5);
        _projectService.AddTask(_systemManager, _projectDto , TaskDTO.ToEntity(task));
        task = _projectService.GetTasksByProject(_projectDto)![0].FromEntity().Item1;

        var resource = new ResourceDTO
        {
            Name = "Recurso",
            Description = "keloke",
            Type = "fiumbaResource",
            Quantity = 1
        };
        
        var resourceService = ResourceService.GetNewInstance(_contextFactory);

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectLeader);

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);
        
        resourceService.CreateResource(_systemManager, resource, null, 1);
        
        var resourceAdder = new ResourceService.ResourceAdder
        {
            ProjectDto = _projectService.GetProjectDto(_project.Name).Item1!,
            ResourceDto = ResourceDTO.ToEntity(_db.Resources.FindBy(r => r.Name == resource.Name)!),
            TaskDto = TaskDTO.ToEntity(task!),
            Quantity = 1
        };
        
        resourceService.AddResourceToTask(_systemManager, resourceAdder);
        
        var (downloader, error) = _projectService.ExportProject(_userDto, false);
        
        var content = downloader!.Value.Content;
        var mimeType = downloader.Value.MimeType;
        var fileName = downloader.Value.FileName;
        
        Assert.IsTrue(error is null &&
                      mimeType == "application/csv" &&
                      fileName == "projects.csv" &&
                      content.Contains("Proyiuto") &&
                      content.Contains("Tarea") &&
                      content.Contains("Recurso") &&
                      content.Contains(DateTime.Today.ToString("MM/dd/yyyy")));
    }

    [TestMethod]
    public void SendNotification_ToManager()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        var taskDto = TaskDTO.ToEntity(task);

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);

        _projectService.AddTask(_managerDto, _projectDto, taskDto);

        _projectService.CalculateEstimation(_projectDto);

        var nots = NotificationService.GetNewInstance(_contextFactory);
        var i = nots.GetUserNotifications(_managerDto).nots!.Length;

        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void SendNotification_ToManagerTwice()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        var taskDto = TaskDTO.ToEntity(task);

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);

        _projectService.AddTask(_managerDto, _projectDto, taskDto);

        _projectService.CalculateEstimation(_projectDto);
        _projectService.CalculateEstimation(_projectDto);

        var nots = NotificationService.GetNewInstance(_contextFactory);
        var i = nots.GetUserNotifications(_managerDto).nots!.Length;

        Assert.IsTrue(i > 0);
    }

    [TestMethod]
    public void GetAllProjectThatAreLeader_returnProjects()
    {
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectLeader);
        _projectService.SetLeaderToProject(_systemManager, _userDto, _projectDto);

        var result = _projectService.GetAllProjectThatUserAreLeader(_userDto);
        Assert.IsTrue(!result.IsNullOrEmpty());
    }

    [TestMethod]
    public void GetAllProjectThatAreManager_returnProjects()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var result = _projectService.GetAllProjectThatUserAreManager(_systemManager);
        Assert.IsTrue(!result.IsNullOrEmpty());
    }

    [TestMethod]
    public void GetAllProjectThatAreMember_returnProjects()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var result = _projectService.GetAllProjectThatUserAreMember(_userDto);
        Assert.IsTrue(!result.IsNullOrEmpty());
    }

    [TestMethod]
    public void SendNotification_ToLeader()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        var taskDto = TaskDTO.ToEntity(task);

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _leaderDto, _projectDto);

        _projectService.AddTask(_managerDto, _projectDto, taskDto);

        _projectService.CalculateEstimation(_projectDto);

        var nots = NotificationService.GetNewInstance(_contextFactory);
        var i = nots.GetUserNotifications(_leaderDto).nots!.Length;

        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void SendNotification_ToMember()
    {
        var task = new ProjectTask("Tarea", "Descripción", 5);
        var taskDto = TaskDTO.ToEntity(task);

        _projectService.AddManagerToProject(_systemManager, _managerDto, _projectDto);
        _projectService.SetLeaderToProject(_systemManager, _leaderDto, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        _projectService.AddTask(_managerDto, _projectDto, taskDto);

        _projectService.CalculateEstimation(_projectDto);

        var nots = NotificationService.GetNewInstance(_contextFactory);
        var i = nots.GetUserNotifications(_userDto).nots!.Length;

        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void AddTaskToProject_TaskGraphReactivity()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;

        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };
        _ = _projectService.AddTask(_systemManager, _projectDto, task);

        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void RemoveTaskFromProject_TaskGraphReactivity()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;

        var task = new TaskDTO
        {
            Title = "tarea",
            Description = "la descripcion",
            DurationHour = 0
        };

        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        _ = _projectService.RemoveTask(_systemManager, _projectDto, tasks[0]);
        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void ChangeTaskDuration_TaskGraphInvoke()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;
        
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1
        };
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);

        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = tasks[0],
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.DurationHour = 2;

        
        _ = _projectService.ChangeTaskDuration(_systemManager, changeTaskHelper);
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.IsTrue(0 < i);
    }

    [TestMethod]
    public void ChangeTaskStateToStarted_TaskGraphInvoke()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;
        
        
        var userService = UserService.GetNewInstance(_contextFactory);
        userService.CreateUser(_systemManager, _systemManager);
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        _projectDto = ProjectDTO.ToEntity(_projectService.GetProject(_projectDto.Name).Item1!);

        var taskDto = new TaskDTO
        {
            Title = "TaskA",
            Description = "Primera tarea",
            DurationHour = 1,
            StartTaskDate = DateTime.Today,
            EndTaskDate = DateTime.Today
        };
        
        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        taskDto = _projectService.GetTasksByProject(_projectDto)?[0];


        var taskDtoNewInfo = new TaskDTO();
        var changeTaskHelper = new ProjectService.ChangeTaskHelper
        {
            OriginalTaskDto = taskDto!,
            NewInfoTaskDto = taskDtoNewInfo,
            ProjectDto = _projectDto
        };
        changeTaskHelper.NewInfoTaskDto.State = TaskState.Started;
        changeTaskHelper.NewInfoTaskDto.StartTaskDate = DateTime.Now;
        _projectService.AddMemberToTask(_systemManager, _projectDto, taskDto, _systemManager);
        
        _ = _projectService.ChangeTaskState(_systemManager, changeTaskHelper);
        
        var result = _projectService.GetTasksByProject(_projectDto)?[0];
        
        Assert.AreEqual(2, i);
    }


    [TestMethod]
    public void AddDepToTask_TaskGraphInvoke()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge()
                {
                    targetDto = tasks[0],
                    depDto = tasks[1],
                    type = TypeDependency.FS
                });

        Assert.AreEqual(3, i);
    }

    [TestMethod]
    public void RemoveDepFromTask_TaskGraphInvoke()
    {
        var i = 0;
        Reactivity.GetInstance().OnChangeTaskGraph += () => i++;

        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        
        var task = new ProjectTask("asdjfasfdjk", "jasdkf", 10);
        var task1 = new ProjectTask("asdajfasfdjkladsjkl", "jasdkf", 10);

        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task));
        _projectService.AddTask(_systemManager, ProjectDTO.ToEntity(_project), TaskDTO.ToEntity(task1));
        
        var taskDto = _projectService.GetTasksByProject(_projectDto)?[0];
        var taskDto1 = _projectService.GetTasksByProject(_projectDto)?[1];

        _projectService.AddDependencyToTask(_systemManager, _projectDto,
                new TaskGraphService.Edge
                {
                    targetDto = taskDto!,
                    depDto = taskDto1!,
                    type = TypeDependency.FS
                });

        var result = _projectService
            .RemoveDependencyFromTask(_systemManager, _projectDto, taskDto!, taskDto1!);

        Assert.AreEqual(4, i);
    }

    [TestMethod]
    public void SetProgrammedDateToStartTask_AssignedCorrectly()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1,
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);

        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, tasks[0], DateTime.Now.AddDays(3));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SetProgrammedDateToStartTask_NotValidStart()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1,
        };

        var task1 = new TaskDTO
        {
            Title = "TareaTest1",
            Description = "Descripción",
            DurationHour = 1,
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        _projectService.AddTask(_systemManager, _projectDto, task1);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddDependencyToTask(_systemManager, _projectDto, new TaskGraphService.Edge()
        {
            depDto = tasks[1],
            type = TypeDependency.FS,
            targetDto = tasks[0],
        });
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);
        

        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, tasks[0], TaskGraphService.GetDateTimeNow().AddDays(1));

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SetProgrammedDateToStartTaskWithOlderDate_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        _projectService.AddMemberToTask(_systemManager, _projectDto, tasks[0], _userDto);

        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, tasks[0], new DateTime(2002, 1, 1));

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void SetProgrammedDateWithUnassignedUserToTask_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, tasks[0], DateTime.Today);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void SetProgrammedDateWithNonTrackTask_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        _projectService.AddMemberToProject(_systemManager, _userDto, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1
        };

        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, task, DateTime.Today);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void SetProgrammedDateWithNonTrackProject_ReturnError()
    {
        _projectDto.Name = "asdjkfas";

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1,
        };

        var result = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, task, DateTime.Today);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void SetProgrammedDateToStartTask_UserNotAssignedToTask_ReturnsError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);

        var task = new TaskDTO
        {
            Title = "TareaTest",
            Description = "Descripción",
            DurationHour = 1
        };

        _projectService.AddTask(_systemManager, _projectDto, task);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        var error = _projectService.SetProgrammedDateToStartTask(_userDto, _projectDto, tasks[0], DateTime.Today);

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void TestIfIntervalCatches_Project()
    {
        Thread.Sleep(800);

        Assert.IsTrue((_project.StartDate - DateTime.Now).TotalSeconds < 1);
    }

    [TestMethod]
    public void CreateProject_With_Interval()
    {
        var project = new Project("pepe", "joeta");
        _projectService.CreateProject(_systemManager, ProjectDTO.ToEntity(project));
        Thread.Sleep(800);

        Assert.IsTrue((project.StartDate - DateTime.Now).TotalSeconds < 1);
    }

    [TestMethod]
    public void GetAllTasksUsingNewResource_ResturnNull()
    {
        ResourceDTO resourceDto = new ResourceDTO();
        resourceDto.Name = "keloke";
        resourceDto.Description = "keloke";
        resourceDto.Id = 1;
        resourceDto.Quantity = 5;
        resourceDto.Type = "fiumbaResource";
        var tasks = _projectService.GetAllTasksUsingResource(resourceDto.FromEntity().Item1);

        Assert.AreEqual(0, tasks.Count);
    }

    [TestMethod]
    public void GetAllTasksUsingATrackedResource_ResturnEmptyList()
    {
        ResourceDTO resourceDto = new ResourceDTO();
        resourceDto.Name = "keloke";
        resourceDto.Description = "keloke";
        resourceDto.Id = 1;
        resourceDto.Quantity = 5;
        resourceDto.Type = "fiumbaResource";

        var resService = ResourceService.GetNewInstance(_contextFactory);
        resService.CreateResource(_systemManager, resourceDto, null, 5);
        resourceDto = resService.GetAllResourcesDto().Last();
        var tasks = _projectService.GetAllTasksUsingResource(resourceDto.FromEntity().Item1!);

        Assert.AreEqual(0, tasks.Count);
    }

    [TestMethod]
    public void GetAllTasksUsingAResource_ReturnList()
    {
        ResourceDTO resourceDto = new ResourceDTO();
        resourceDto.Name = "keloke";
        resourceDto.Description = "keloke";
        resourceDto.Id = 1;
        resourceDto.Quantity = 5;
        resourceDto.Type = "fiumbaResource";

        TaskDTO taskDto = new TaskDTO();
        taskDto.Title = "Tarea";
        taskDto.Description = "BUenas";
        taskDto.DurationHour = 1;

        var resService = ResourceService.GetNewInstance(_contextFactory);
        resService.CreateResource(_systemManager, resourceDto, null, 5);
        resourceDto = resService.GetAllResourcesDto().Last();

        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        resService.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            ProjectDto = _projectDto,
            TaskDto = tasks[0],
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        var taskss = _projectService.GetAllTasksUsingResource(resourceDto.FromEntity().Item1);

        Assert.AreEqual(1, taskss.Count);
    }

    [TestMethod]
    public void GetAllTasksUsingAResource_InvalidResource()
    {
        ResourceDTO resourceDto = new ResourceDTO();
        resourceDto.Name = "keloke";
        resourceDto.Description = "keloke";
        resourceDto.Id = 1;
        resourceDto.Quantity = 5;
        resourceDto.Type = "fiumbaResource";

        TaskDTO taskDto = new TaskDTO();
        taskDto.Title = "Tarea";
        taskDto.Description = "BUenas";
        taskDto.DurationHour = 1;
        
        var resService = ResourceService.GetNewInstance(_contextFactory);
        resService.CreateResource(_systemManager, resourceDto, null, 5);
        resourceDto = resService.GetAllResourcesDto().Last();

        _projectService.AddTask(_systemManager, _projectDto, taskDto);
        var tasks = _projectService.GetTasksByProject(_projectDto);
        
        resService.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            ProjectDto = _projectDto,
            TaskDto = tasks[0],
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        var tasksRelated = _projectService.GetAllTasksUsingResource(new Resource("a", "b", "c", 1));

        Assert.AreEqual(0, tasksRelated.Count);
    }
    
    [TestMethod]
    public void CreateNewProjectService_ReturnNewEmptyList()
    {
        var projectService = ProjectService.GetNewInstance();
        projectService = ProjectService.GetNewInstance(_contextFactory);
        Assert.IsNotNull(projectService);    
    }

    [TestMethod]
    public void AddManagerToProjectWithManager_ReturnError()
    {
        _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        var result = _projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
        Assert.IsInstanceOfType<Error?>(result);
    }
    
}
