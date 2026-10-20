using System.Globalization;
using Auxiliaries;
using Domain;
using Services.DTOs;

namespace Tests;

[TestClass]
public class ProjectDtoTest
{

    [TestMethod]
    public void ToEntity_ConvertCorrectly()
    {
        var result = ProjectDTO.ToEntity(new Project("breaaa", "teretetete"));
        
        Assert.IsNotNull(result);
        
    }
    
    [TestMethod]
    public void FromEntity_ConvertCorrectly()
    {
        var projectDto = new ProjectDTO()
        {
            Name = "tata",
            Description = "teretetete",
        }; 
        
        var result = projectDto.FromEntity().Item1;
        Assert.IsInstanceOfType<Project>(result);
    }
    
    [TestMethod]
    public void FromEntityEmptyName_ReturnError()
    {
        ProjectDTO projectDto = new ProjectDTO
        {
            Name = "hiphip",
            Description = new string('a', 1000)
        };

        Error? result = projectDto.FromEntity().Item2;
        
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ToEntity_ConvertCorrectly_HaveMember()
    {
        var project = new Project("nombreProyecto", "descripcionProyecto");
        var member = new User("Asd","asdzx","asd@asd.com", DateTime.Parse("2/2/2002",CultureInfo.CreateSpecificCulture("en-US")), [2,3,2]);
        project.Team.AddMember(member);
        
        var projectDto = ProjectDTO.ToEntity(project);
        var result = projectDto.Team.Item2.Values.First();
        
        Assert.IsInstanceOfType<UserDTO>(result);
    }

    [TestMethod]
    public void ToEntity_ConvertCorrectly_HaveManager()
    {
        var project = new Project("nombreProyecto", "descripcionProyecto");
        var manager = new User("pripichi",
            "asdzx",
            "piripichi@asd.com",
            DateTime.Parse("2/2/2002",
                CultureInfo.CreateSpecificCulture("en-US")),
            [
                2,
                3,
                2
            ]);
        
        manager.Role = Roles.SystemManager;
        project.Team.Manager = manager;
        var projectDto = ProjectDTO.ToEntity(project);
        var managerDto = projectDto.Team.Item1;
        
        Assert.IsInstanceOfType<UserDTO>(managerDto);
    }

    [TestMethod]
    public void ToEntity_ConvertCorrectly_HaveTask()
    {
        var project = new Project("nombreProyecto", "descripcionProyecto");
        var task = new ProjectTask("titulin","Tremendo Titulo", 10);
        
        project.AddTaskToList(task);
        var projectDto = ProjectDTO.ToEntity(project);
        
        Assert.IsInstanceOfType<TaskDTO>(projectDto.Tasks.First());
    }
    
    [TestMethod]
    public void ToEntity_ConvertCorrectly_HaveResource()
    {
        var project = new Project("nombreProyecto", "descripcionProyecto");
        var task = new ProjectTask("titulin","Tremendo Titulo", 10);
        var resource = new Resource("pepe","pepe","pepe", 1);
        
        project.AddTaskToList(task);
        project.AddResourceRelation("titulin",resource,1);
        
        var projectDto = ProjectDTO.ToEntity(project);
        
        Assert.IsInstanceOfType<ResourceDTO>(projectDto.ResourceRelation.First().Value.First().Item1);
    }
    
    [TestMethod]
    public void ToEntity_ConvertCorrectly_HaveExclusiveResource()
    {
        var project = new Project("nombreProyecto", "descripcionProyecto");
        var task = new ProjectTask("titulin","Tremendo Titulo", 10);
        var resource = new Resource("pepe","pepe","pepe", 2);
        
        project.AddTaskToList(task);
        project.AddResourceExclusive(resource, 2);
        
        var projectDto = ProjectDTO.ToEntity(project);
        
        Assert.IsInstanceOfType<(ResourceDTO, int)>(projectDto.ResourceExclucive.First());
    }

    [TestMethod]
    public void TaskToUsersTOTaskToUsersDTO_IsNotNull()
    {
        var project = new Project("hola", "adskj");
        
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());
        var user1 = new User("peo", "pep", "pe1j@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);
        team.AddMember(user1);

        project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        project.AddTaskToList(task);
        project.AddTaskToList(task1);

        project.AssignedUserToTask(task, user1);
        project.AssignedUserToTask(task1, user);
        
        var projectDto = ProjectDTO.ToEntity(project);
        var result = projectDto.TaskToUsers;
        
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ToEntity_CorrectEndDate()
    {
        var project = new Project("hola", "adskj")
        {
            EndDate = DateTime.MaxValue
        };

        var result = ProjectDTO.ToEntity(project);
        
        Assert.AreEqual(DateTime.MaxValue, result.EndDate);
    }

    [TestMethod]
    public void GetId()
    {
        var project = new Project("hola", "adskj")
        {
            EndDate = DateTime.MaxValue,
            Id = 1
        };
        Assert.IsTrue(project.Id == 1);
    }
}