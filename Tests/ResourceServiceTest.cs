using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Domain.HelpersDB;
using DomainConstruct;
using Services;
using Services.DTOs;

namespace Tests;

[TestClass]
public class ResourceServiceTest
{
     private PureDatabase _db;
     
     private ResourceService _resourceService = null!;
     private UserDTO _userDto = null!;
     private UserDTO _systemManager = null!;
     private ProjectDTO _projectDto = null!;
     private Project _project = null!;
     private ResourceDTO _resourceDto = null!;
     private Resource _resource5 = null!;
     private Resource _resource1 = null!;
     ResourceService.ResourceAdder _resourceData;
     private ResourceDTO _resourceDtoTest = null!;
     private ISqlContextFactory _contextFactory;
     private static int _i = 0;
     
     [TestInitialize]
     public void Initialize()
     {
         _i++;
         _contextFactory = new InMemorySqlContextFactory("ResourceControllerTest" + _i);
         _db = PureDatabase.GetNewInstance(_contextFactory);
         
         _resourceService = ResourceService.GetNewInstance(_contextFactory);
         
         _resourceData = new ResourceService.ResourceAdder();
         
         _resourceDto = new ResourceDTO
         {
             Name = "Recurs",
             Type = "Category2",
             Quantity = 2,
         };

         _resource5 = new Resource("Recurs","Category2","", 2);
         
         
         _db.Resources.Add(_resource5);
         
         _resource1 = new Resource("Requiem","Category1","", 1);
         _db.Resources.Add(_resource1);
         
         _project = new Project("Proyiuto", "Descricricri");
         _db.Projects.Add(_project);

         _projectDto = ProjectDTO.ToEntity(
             ProjectConstruct.ConstructDeep(
                 _contextFactory.Generate().Project.First(p => p.Name == _project.Name),
                 _contextFactory.Generate()
                 )
             );

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

         _systemManager = new UserDTO
         {
             Name = "System",
             LastName = "Manager",
             Email = "test2@test.com",
             BirthDate = "04/18/2003",
             Password = "123456Ac@",
             Role = Roles.SystemManager
         };

         var systemManager = _systemManager.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
         systemManager.Role = Roles.SystemManager;
         _db.Users.Add(systemManager);
         _systemManager = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _systemManager.Email)!);
         
         _resourceDtoTest = ResourceDTO.ToEntity(_resource1);

         _db.Users.Add(systemManager);
     }

     [TestCleanup]
     public void Cleanup()
     {
         _contextFactory.Generate().Database.EnsureDeleted();
     }
     
     [TestMethod]
     public void CreateResourceService_IsNotNull()
     {
         Assert.IsNotNull(_resourceService);
     }

     [TestMethod]
     public void CreateResourceAdder_IsNotNull()
     {
         ResourceService.ResourceAdder adder;

         adder = new ResourceService.ResourceAdder
         {
             ResourceDto = _resourceDto,
             Quantity = 1,
             ProjectDto = _projectDto,
             TaskDto = new TaskDTO()
         };
         
         Assert.IsNotNull(adder);
     }
     
     [TestMethod]
     public void CreateResourceWithSystemManager_CratesCorrectly()
     {
         var resourceDtoTest = new ResourceDTO();
         resourceDtoTest.Name= " Lalo";
         resourceDtoTest.Description = "Lipo";
         resourceDtoTest.Type = "Gilda";
         
         var error = _resourceService.CreateResource(_systemManager, resourceDtoTest, null, 1); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void CreateResourceWithSystemManagerExclusiveToProject_CratesCorrectly()
     {
         var resourceDtoTest = new ResourceDTO
         {
             Name = " Lalo",
             Description = "Lipo",
             Type = "Gilda"
         };

         var error = _resourceService.CreateResource(_systemManager, resourceDtoTest, _projectDto, 1); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void CreateResourceWithSystemManagerExclusiveToNotTrackedProject_ReturnError()
     {
         var resourceDtoTest = new ResourceDTO();
         var projectDtoTest = new ProjectDTO();
         
         resourceDtoTest.Name= " Lalo";
         resourceDtoTest.Description = "Lipo";
         resourceDtoTest.Type = "Gilda";
         projectDtoTest.Name = "Lalo";
         
         var error = _resourceService.CreateResource(_systemManager, resourceDtoTest, projectDtoTest, 1); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void CreateResourceWithSystemManagerRepeated_ReturnError()
     {
         var error = _resourceService.CreateResource(_systemManager, _resourceDto, null, 1); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void CreateResourceWithProjectMember_CratesWrongly()
     {
        var error = _resourceService.CreateResource(_userDto, _resourceDto, null, 1); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void CreateResourceWithEmpty_CratesWrongly()
     {
         _resourceDto.Name = "";
         
         var error = _resourceService.CreateResource(_systemManager, _resourceDto, null, 1); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void CreateResourceWithNonTrackedUser_CratesWrongly()
     {
         var userDto = new UserDTO
         {
             Name = "pancho",
             Email = "lolito@mail.com"
         };

         var error = _resourceService.CreateResource(userDto, _resourceDto, null, 1); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void CreateResourceWithNotTrackedResource_CratesCorrectly()
     {
         var resourceDto = new ResourceDTO
         {
             Name = " Lalo",
             Description = "Crack",
             Type = "Gilda"
         };

         var error = _resourceService.CreateResource(_systemManager, resourceDto, null, 1); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void DeleteResourceWithSystemManager_IsNull()
     {
         var resourceTest = new Resource("Test", "Category2", "desc", 1);
         
         _db.Resources.Add(resourceTest);
         
         var error = _resourceService.DeleteResource(_systemManager, resourceTest.Id); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void DeleteResourceWithProjectMember_ReturnsError()
     {
         var error = _resourceService.DeleteResource(_userDto, _resourceDtoTest.Id); 
         
         Assert.IsInstanceOfType<Error>(error);
     }

     [TestMethod]
     public void DeleteResourceWithNotTrackedResource_ReturnError()
     {
         var resourceDto = new ResourceDTO
         {
             Name = "Clerigo",
             Type = "Sanders",
             Id = 333
         };

         var error = _resourceService.DeleteResource(_systemManager, resourceDto.Id); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void DeleteResourceWithOneMultiplicity_ReturnNull()
     {
         var error = _resourceService.DeleteResource(_systemManager, _resourceDtoTest.Id); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void DeleteResourceWithOneMultiplicityNotBeingUsed_ReturnNull()
     {
         var taskTest = new ProjectTask("Requiem", "Jairson", 10);
         _project.AddTaskToList(taskTest);

         var resource = _resourceDtoTest.FromEntity().Item1;
         
         if (resource == null) return;
         _project.AddResourceRelation("Requiem", resource, 1);
         _db.Projects.Update(_project);
         
         var error = _resourceService.DeleteResource(_systemManager, _resourceDtoTest.Id); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void DeleteResourceWithOneMultiplicityBeingUsed_ReturnError()
     {
         
         var taskTest = new ProjectTask("Requiem", "Jairson", 10);
         
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         projectService.AddManagerToProject(_systemManager, _systemManager, _projectDto);
         
         projectService.AddTask(_systemManager, _projectDto, TaskDTO.ToEntity(taskTest));

         taskTest = _contextFactory.Generate().ProjectTask.First(t => t.Title == taskTest.Title && t.ProjectId == _projectDto.Id);
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource1);
         _resourceData.ProjectDto = ProjectDTO.ToEntity(_project);
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = TaskDTO.ToEntity(taskTest);
         
         _resourceService.AddResourceToTask(_systemManager, _resourceData);

         projectService.ChangeTaskState(_systemManager, new ProjectService.ChangeTaskHelper()
         {
             OriginalTaskDto = TaskDTO.ToEntity(taskTest),
             ProjectDto = _projectDto,
             NewInfoTaskDto = new TaskDTO()
             {
                 State = TaskState.Started,
                 StartTaskDate = DateTime.Now,
             }
         });
         
         var error = _resourceService.DeleteResource(_systemManager, _resource1.Id);

         Assert.IsInstanceOfType<Error>(error);
     }

     [TestMethod]
     public void GetResourceDTO_AreEqualToOriginalResource()
     {
         var resourceLst = _resourceService.GetResourceDtoLst(_resource5.Type).Item1;
         var resourceType = resourceLst[0].Type;
         
         var (_, error) = _resourceService.GetResourceDtoLst(resourceType);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void GetResourceDTOWithNonTrackedName_ReturnError()
     {
         var resourceName ="Papelistri";
         
         var (_, error) = _resourceService.GetResourceDtoLst(resourceName);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeNameResourceWithSystemManager_AssignCorrectly()
     {
         var resourceTest = new Resource("Test", "Category2", "desc", 1);
         
         _db.Resources.Update(resourceTest);
         var error = _resourceService.ChangeNameResource(_systemManager, _projectDto, _resourceDto, "Ramon"); 
         
         Assert.IsNull(error);
     }

     [TestMethod]
     public void ChangeNameResourceWithProjectMember_ReturnsError()
     {
         var error = _resourceService.ChangeNameResource(_userDto, _projectDto, _resourceDto, "Ramon"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeNameResourceWithEmptyOldName_ReturnsError()
     {
         var resourceDto = new ResourceDTO
         {
             Type = ""
         };


         var error = _resourceService.ChangeNameResource(_systemManager, _projectDto, resourceDto, "Ramon"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeNameResourceWithEmptyNewName_ReturnsError()
     {
         var error = _resourceService.ChangeNameResource(_systemManager, _projectDto, _resourceDto, ""); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeNameResourceWithProjectManagerNotUsedInOtherProject_ReturnsNull()
     {
         var projectManager = new User("Manager",
             "Stabilo",
             "sharpi@mail.com",
             DateTime.Parse("10/10/2000",
                 CultureInfo.CreateSpecificCulture("en-US")),
             new byte[4]);
         projectManager.Role = Roles.ProjectManager;
         _project.Team.Manager = projectManager;
         _db.Users.Add(projectManager);
         
         var error = _resourceService.ChangeNameResource(UserDTO.ToEntity(projectManager), _projectDto,ResourceDTO.ToEntity(_resource1), "imposibol"); 
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void ChangeNameResourceWithProjectManagerUsedInOtherProject_ReturnsError()
     {
         var projectManager = new User("Manager", "Stabilo", "sharpi@mail.com", DateTime.Parse("10/10/2000",CultureInfo.CreateSpecificCulture("en-US")), new byte[4]);
         var projectTest = new Project("Presunto", "Deskibidi");
         var taskTest = new ProjectTask("Requiem", "Jairson", 10);
         projectManager.Role = Roles.ProjectManager;
         
         _db.Users.Add(projectManager);
         
         _project.Team.Manager = projectManager;
         
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, ProjectDTO.ToEntity(projectTest));
         projectService.AddTask(_systemManager, ProjectDTO.ToEntity(projectTest), TaskDTO.ToEntity(taskTest));
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource1);
         _resourceData.ProjectDto = ProjectDTO.ToEntity(projectTest);
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = TaskDTO.ToEntity(taskTest);
         
         _resourceService.AddResourceToTask(_systemManager, _resourceData);
         projectService.AddTask(_systemManager, ProjectDTO.ToEntity(projectTest), TaskDTO.ToEntity(taskTest));
         projectTest.AddTaskToList(taskTest);
         projectTest.AddResourceRelation("Requiem", _resource1, 1);
         
         _db.Projects.Add(projectTest);
         var error = _resourceService.ChangeNameResource(UserDTO.ToEntity(projectManager), ProjectDTO.ToEntity(projectTest), ResourceDTO.ToEntity(_resource1), "imposibol"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeDescResourceWithSystemManager_AssignCorrectly()
     {
         var resourceTest = new Resource("Test", "Category2", "desc", 1);
         
         _db.Resources.Add(resourceTest);
         
         var error = _resourceService.ChangeDescriptionResource(_systemManager, _projectDto, _resourceDto, "Ramon"); 
         
         Assert.IsNull(error);
     }

     [TestMethod]
     public void ChangeDescResourceWithProjectMember_ReturnsError()
     {
         var error = _resourceService.ChangeDescriptionResource(_userDto, _projectDto, _resourceDto, "Ramon"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeDescResourceWithEmptyOldName_ReturnsError()
     {
         var resourceDto = new ResourceDTO
         {
             Type = ""
         };

         var error = _resourceService.ChangeDescriptionResource(_systemManager, _projectDto, resourceDto, "Ramon"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void ChangeDescResourceWithEmptyNewName_ReturnsError()
     {
         var error = _resourceService.ChangeDescriptionResource(_systemManager, _projectDto, _resourceDto, ""); 
         
         Assert.IsInstanceOfType<Error>(error);
     }

     [TestMethod]
     public void ChangeDescResourceWithProjectManagerNotUsedInOtherProject_ReturnsNull()
     {
         var projectManager = new User("Manager", "Stabilo", "sharpi@mail.com",
             DateTime.Parse("10/10/2000", CultureInfo.CreateSpecificCulture("en-US")), new byte[4]);
         projectManager.Role = Roles.ProjectManager;
         _project.Team.Manager = projectManager;
         _db.Users.Add(projectManager);
         
         var error = _resourceService.ChangeDescriptionResource(UserDTO.ToEntity(projectManager), _projectDto,
             ResourceDTO.ToEntity(_resource1), "imposibol");
         
         Assert.IsNull(error);
     }

     [TestMethod]
     public void ChangeDescResourceWithProjectManagerUsedInOtherProject_ReturnsError()
     {
         var projectManager = new User("Manager", "Stabilo", "sharpi@mail.com", DateTime.Parse("10/10/2000",CultureInfo.CreateSpecificCulture("en-US")), new byte[4]);
         var projectTest = new Project("Presunto", "Deskibidi");
         var taskTest = new ProjectTask("Requiem", "Jairson", 10);
         projectManager.Role = Roles.ProjectManager;
         
         _db.Users.Add(projectManager);
         
         _project.Team.Manager = projectManager;
         
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, ProjectDTO.ToEntity(projectTest));
         projectService.AddTask(_systemManager, ProjectDTO.ToEntity(projectTest), TaskDTO.ToEntity(taskTest));
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource1);
         _resourceData.ProjectDto = ProjectDTO.ToEntity(projectTest);
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = TaskDTO.ToEntity(taskTest);
         
         _resourceService.AddResourceToTask(_systemManager, _resourceData);
         projectService.AddTask(_systemManager, ProjectDTO.ToEntity(projectTest), TaskDTO.ToEntity(taskTest));
         projectTest.AddTaskToList(taskTest);
         projectTest.AddResourceRelation("Requiem", _resource1, 1);
         
         _db.Projects.Add(projectTest);
         var error = _resourceService.ChangeDescriptionResource(UserDTO.ToEntity(projectManager), ProjectDTO.ToEntity(projectTest), ResourceDTO.ToEntity(_resource1), "imposibol"); 
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithSystemManager_AssignCorrectly()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };

         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithNotTrackedResource_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var resourceDtoTest = new ResourceDTO();
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         resourceDtoTest.Type = "Category1";
         resourceDtoTest.Name = "Hola";
         
         _resourceData.ResourceDto = resourceDtoTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithNotTrackedResourceType_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var resourceDtoTest = new ResourceDTO();
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         resourceDtoTest.Type = "jairson";
         
         
         _resourceData.ResourceDto = resourceDtoTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithNotTrackedProject_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var projectDtoTest = new ProjectDTO
         {
             Name = "pellistri"
         };

         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = projectDtoTest;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithProjectMember_ReturnError()
     {
         
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         
         var error = _resourceService.AddResourceToTask(_userDto,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithExclusiveUsedResource_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("Presunto", "Deskibidi");
         
         _resource5.ToggleExclusiveness();
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         var (task, _) = taskDtoTest.FromEntity();
         _db.Projects.Add(projectTest);
         
         if (task == null) return;
         projectTest.AddTaskToList(task);
         projectTest.AddResourceRelation("Dua lipa", _resource5, 1);
         _project.AddResourceExclusive(_resource5, 1);

         _project.AddTaskToList(task);

         _resourceDto.IsExclusive = true;
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         
         _resourceService.AddResourceToTask(_systemManager,_resourceData);
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
      
     [TestMethod]
     public void AddResourceNotRelatedWithExclusiveUsedResource_ReturnError()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };
         var projectTest = new Project("Presunto", "Deskibidi");
         var projectService = ProjectService.GetNewInstance(_contextFactory);

         
         _resource5.ToggleExclusiveness();
         _db.Resources.Update(_resource5);
         
         _resourceService.CreateResource(_systemManager, _resourceDto, ProjectDTO.ToEntity(projectTest), 1);
         projectService.CreateProject(_systemManager, _projectDto);
         _projectDto = projectService.GetProjectDto(_projectDto.Name).Item1!;
         projectService.CreateProject(_systemManager, ProjectDTO.ToEntity(projectTest));
         
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         projectService.AddTask(_systemManager, ProjectDTO.ToEntity(projectTest), taskDtoTest);
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource5);
         _resourceData.ProjectDto = ProjectDTO.ToEntity(projectTest);
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = projectService.GetTasksByProject(ProjectDTO.ToEntity(projectTest))!.First();
         
         _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         var (task, _) = taskDtoTest.FromEntity();
         if (task == null) return;
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource5);
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = projectService.GetTasksByProject(_projectDto)!.First();
         
         _resourceService.AddResourceToTask(_systemManager,_resourceData);
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithNonTrackedTask_ReturnError()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };

         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         taskDtoTest.Title = "Revel";
         _resourceData.TaskDto = taskDtoTest;
         
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithNonPendingState_ReturnsError()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("Presunto", "Deskibidi");
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         _projectDto = projectService.GetProjectDto(_projectDto.Name).Item1!;
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         taskDtoTest = projectService.GetTasksByProject(_projectDto)![0];
         var (task, _) = taskDtoTest.FromEntity();
         _db.Projects.Add(projectTest);
         if (task == null) return;
         projectTest.AddTaskToList(task);
         projectTest.AddResourceRelation("Dua lipa", _resource5, 1);

         projectService.ChangeTaskState(_systemManager, new ProjectService.ChangeTaskHelper()
         {
             OriginalTaskDto = taskDtoTest,
             ProjectDto = _projectDto,
             NewInfoTaskDto = new TaskDTO()
             {
                 State = TaskState.Started,
                 StartTaskDate = DateTime.Now,
             }
         });

         _project.AddTaskToList(task);
         _resource5.Exclusive = true;
         _db.Resources.Update(_resource5);
         _resourceDto.IsExclusive = true;
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
             
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithExclusiveResource_ReturnNull()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("Presunto", "Deskibidi");
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         var (task, _) = taskDtoTest.FromEntity();
         _db.Projects.Add(projectTest);
         if (task == null) return;
         projectTest.AddTaskToList(task);
         projectTest.AddResourceRelation("Dua lipa", _resource5, 1);

         _project.AddTaskToList(task);
         _resource5.Exclusive = true;
         _db.Resources.Update(_resource5);
         _resourceDto.IsExclusive = true;
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
             
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithExclusiveResource_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("Presunto", "Deskibidi");
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         var (task, _) = taskDtoTest.FromEntity();
         _db.Projects.Add(projectTest);
         if (task == null) return;
         projectTest.AddTaskToList(task);
         projectTest.AddResourceRelation("Dua lipa", _resource5, 1);

         _project.AddTaskToList(task);
         _resource5.Exclusive = true;
         _db.Resources.Update(_resource5);
         _resourceDto.IsExclusive = true;
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
             
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void AddResourceRelationWithExclusiveResourceInUse_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("Presunto", "Deskibidi");
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);
         var (task, _) = taskDtoTest.FromEntity();
         _db.Projects.Add(projectTest);
         if (task == null) return;
         projectTest.AddTaskToList(task);

         _project.AddTaskToList(task);
         _resource5.Exclusive = true;
         _db.Resources.Update(_resource5);
         _resourceDto.IsExclusive = true;
         projectTest.AddResourceRelation("Dua lipa", _resource5, 1);
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;

         _resourceService.AddResourceToTask(_systemManager,_resourceData);
         _project.AddResourceExclusive(_resource5, 1);
         _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         var error = _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithSystemManager_ReturnNull()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);

         var tasks = projectService.GetTasksByProject(_projectDto);

         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         var resourceTuple = resourceTest.FromEntity();
         _resourceData.ResourceDto = resourceTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = tasks[0];
         if (resourceTuple.Item1 != null) _resourceService.AddResourceToTask(_systemManager,_resourceData);
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithNoResources_ReturnError()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);

         var tasks = projectService.GetTasksByProject(_projectDto);

         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         _resourceData.ResourceDto = resourceTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = tasks[0];
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }

     
     [TestMethod]
     public void RemoveResourceManyInstances_ReturnNull()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);

         var tasks = projectService.GetTasksByProject(_projectDto);
         
         _resourceDto.Quantity = 2;
         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         var resourceTuple = resourceTest.FromEntity();
         _resourceData.ResourceDto = resourceTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 2;
         _resourceData.TaskDto = tasks[0];
         if (resourceTuple.Item1 != null) _resourceService.AddResourceToTask(_systemManager, _resourceData);
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithProjectMember_ReturnError()
     {
         var error = _resourceService.RemoveResourceFromTask(_userDto,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithNotTrackedTask_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var taskDtoObj = new TaskDTO();
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         taskDtoObj.Title = "Lol";
         taskDtoObj.Description = "Gaga";
         taskDtoObj.DurationHour = 12;
         
         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         var resourceTuple = resourceTest.FromEntity();
         if (resourceTuple.Item1 != null) _project.AddResourceRelation("Dua lipa", resourceTuple.Item1, 2);
         _resourceData.ResourceDto = resourceTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 2;
         _resourceData.TaskDto = taskDtoObj;
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithNotTrackedResource_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var resourceTest = new Resource("papel", "trapo", "Random", 1);
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         _project.AddResourceRelation("Dua lipa", _resource1, 2);
         _resourceData.ResourceDto = ResourceDTO.ToEntity(resourceTest);
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 2;
         _resourceData.TaskDto = taskDtoTest;
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithNotTrackedProject_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var projectTest = new Project("papel", "trapo");
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         _project.AddResourceRelation("Dua lipa", _resource1, 2);
         _resourceData.ResourceDto = _resourceDto;
         _resourceData.ProjectDto = ProjectDTO.ToEntity(projectTest);
         _resourceData.Quantity = 2;
         _resourceData.TaskDto = taskDtoTest;
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
    
     [TestMethod]
     public void RemoveResourceFromTaskWithNoExistentResources_ReturnError()
     {
         var taskDtoTest = new TaskDTO();
         var taskDtoObj = new TaskDTO();
         _ = new Project("papel", "trapo");
         
         taskDtoTest.Title = "Dua lipa";
         taskDtoTest.Description = "Houdini";
         taskDtoTest.DurationHour = 10;
         
         taskDtoObj.Title = "Grandson";
         taskDtoObj.Description = "Space";
         taskDtoObj.DurationHour = 10;
         
         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var (taskObj, _) = taskDtoObj.FromEntity();
         if (taskObj != null) _project.AddTaskToList(taskObj);

         _project.AddResourceRelation("Dua lipa", _resource5, 2);
         
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource1);
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 2;
         _resourceData.TaskDto = taskDtoTest;
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskWithSystemManagerQuantityOne_ReturnNull()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };
         var projectService = ProjectService.GetNewInstance(_contextFactory);
         projectService.CreateProject(_systemManager, _projectDto);
         projectService.AddTask(_systemManager, _projectDto, taskDtoTest);

         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         var resourceTuple = resourceTest.FromEntity();
         _resourceData.ResourceDto = resourceTest;
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         if (resourceTuple.Item1 != null) _resourceService.AddResourceToTask(_systemManager, _resourceData);
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsNull(error);
     }
     
     [TestMethod]
     public void RemoveResourceFromTaskNotRelatedToProject_ReturnError()
     {
         var taskDtoTest = new TaskDTO
         {
             Title = "Dua lipa",
             Description = "Houdini",
             DurationHour = 10
         };

         var (task, _) = taskDtoTest.FromEntity();
         if (task != null) _project.AddTaskToList(task);

         var resourceList = _db.Resources.GetAll();
         
         resourceList.Add(_resource1);
         _db.Resources.Add(_resource1);
         
         _db.Resources.Update(_resource1);

         var resourceTest = _resourceDto;
         resourceTest.IsExclusive = false;
         var resourceTuple = resourceTest.FromEntity();
         if (resourceTuple.Item1 != null) _project.AddResourceRelation("Dua lipa", resourceTuple.Item1, 1);
         _resourceData.ResourceDto = ResourceDTO.ToEntity(_resource1);
         _resourceData.ProjectDto = _projectDto;
         _resourceData.Quantity = 1;
         _resourceData.TaskDto = taskDtoTest;
         
         var error = _resourceService.RemoveResourceFromTask(_systemManager,_resourceData);
         
         Assert.IsInstanceOfType<Error>(error);
     }

     [TestMethod]
     public void GetAllResourcesDtoFromDatabase_ReturnList()
     {
         var listResource = _resourceService.GetAllResourcesDto();
         
         Assert.AreEqual(2, listResource.Count);
     }
     
     [TestMethod]
     public void GetAllResourcesDtoFromNewDatabase_ReturnNewEmptyList()
     {
         ISqlContextFactory _contextFactoryTest = new InMemorySqlContextFactory("jason");
         var resourceService = ResourceService.GetNewInstance(_contextFactoryTest);
         
         var listResource = resourceService.GetAllResourcesDto();
         
         Assert.AreEqual(0, listResource.Count);
     }

     [TestMethod]
     public void CreateNewResourceService_ReturnNewEmptyList()
     {
         var resourceService = ResourceService.GetNewInstance();
         Assert.IsNotNull(resourceService);    
     }
     
     [TestMethod]
     public void GetAllTaskResource_ReturnNewEmptyList()
     {
         ISqlContextFactory _contextFactoryTest = new InMemorySqlContextFactory("jason");
         var resourceService = ResourceService.GetNewInstance(_contextFactoryTest);
         
         var taskResources = resourceService.GetAllTaskResource();
         Assert.AreEqual(0, taskResources.Count);    
     }
     
     [TestMethod]
     public void GetInstanceNewContext_ReturnNewEmptyList()
     {
         ISqlContextFactory _contextFactoryTest = new InMemorySqlContextFactory("jason");
         var resourceService = ResourceService.GetInstance(_contextFactoryTest);
         
         Assert.IsNotNull(resourceService);
     }
     
     [TestMethod]
     public void ResourceIsUsedWithNotTrackedProject_ReturnError()
     {
         bool notUsed = _resourceService.ResourceIsUsed(_resourceDto, new ProjectDTO() {Name ="papel", Description = "trapo"});
         Assert.IsFalse(notUsed);    
     }
}