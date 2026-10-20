using System.Linq.Expressions;
using Auxiliaries;
using Controllers.DBControllers;
using DataAccess;
using DBContext;
using Domain;
using DomainConstruct;
using Microsoft.IdentityModel.Tokens;
using Services.DTOs;

namespace TestDB;

[TestClass]
public class ProjectControllerTest
{
    private RepositoryEf<Project, int> _repository = null!;
    private ISqlContextFactory _contextFactory = null!;
    private ProjectController _controller;
    private int _i = 0;

    [TestInitialize]
    public void Setup()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("ProjectControllerTest" + _i);
        _repository = new RepositoryEf<Project, int>(_contextFactory, UniqueNameRule, GetKeyValue, ProjectConstruct.ConstructShallow);
        _controller = new ProjectController(_repository);
        return;

        Expression<Func<Project, bool>> UniqueNameRule(Project project) => p => p.Name == project.Name && p.Id != project.Id;
        int GetKeyValue(Project project) => project.Id;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void AddProject_AddProjectCorrectly()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;

        project.FromEntity().Item1.AddTaskToList(taskDto.FromEntity().Item1);

        var error = _controller.Add(project);

        Assert.IsNull(error);
    }

    [TestMethod]
    public void AddProjectWithTrackName_ReturnError()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var error = _controller.Add(project);

        Assert.IsInstanceOfType<Error>(error);
    }


    [TestMethod]
    public void FindById_ReturnProject()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var result = _controller.FindBy(p => p.Name == project.Name);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void FindByIdProjectNonTrackName_ReturnNull()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        var result = _controller.FindBy(p => p.Name == project.Name);

        Assert.IsNull(result);
    }


    [TestMethod]
    public void DeleteProjectTrackName_ReturnTrue()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var projectAux = _controller.FindBy(p => p.Name == project.Name);
        if (projectAux == null) return;

        var deleted = _controller.Remove(projectAux.Id);

        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void GetById_ReturnProject()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var result = _controller.GetById(1);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetById_ReturnNull()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        var result = _controller.GetById(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetAllWithEmptyProjects_ReturnNull()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        var result = _controller.GetAll();

        Assert.IsTrue(result.IsNullOrEmpty());
    }

    [TestMethod]
    public void GetAllWithProjects_ReturnProjects()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var result = _controller.GetAll();

        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public void DeleteProjectNonTrackName_ReturnFalse()
    {
        var deleted = _controller.Remove(2);

        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public void UpdateWithNonTrackProject_ReturnError()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        var result = _controller.Update(project);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UpdateProject_UpdateCorrectly()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var projectAux = _controller.FindBy(p => p.Name == project.Name);


        var pureProject = new Project(projectAux.Name, projectAux.Description);
        pureProject.Id = projectAux.Id;
        pureProject.Name = "pepinillo";

        _ = _controller.Update(ProjectDTO.ToEntity(pureProject));

        var updatedProject = _controller.FindBy(p => p.Name == pureProject.Name);
        Assert.AreNotEqual(project.Name, updatedProject!.Name);
    }

    [TestMethod]
    public void ExistWithTrackProject_ReturnTrue()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        _controller.Add(project);

        var result = _controller.Exists(p => p.Name == project.Name);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ExistWithNonTrackProject_ReturnTrue()
    {
        var project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion"
        };

        var result = _controller.Exists(p => p.Name == project.Name);

        Assert.IsFalse(result);
    }
}
