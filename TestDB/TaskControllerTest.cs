using System.Linq.Expressions;
using Auxiliaries;
using Controllers.DBControllers;
using DataAccess;
using DBContext;
using Domain;
using Microsoft.IdentityModel.Tokens;
using Services.DTOs;

namespace TestDB;

[TestClass]
public class TaskControllerTest
{
    private RepositoryEf<ProjectTask, int> _repository = null!; 
    private ISqlContextFactory _contextFactory = null!;
    private TaskController _controller;
    private ProjectDTO _project;
    private int _i = 0;
    
    [TestInitialize]
    public void Setup()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("TaskControllerTest" + _i);
        _repository = new RepositoryEf<ProjectTask, int>(_contextFactory, UniqueNameRule, GetKeyValue);
        _controller = new TaskController(_repository);
        
        _project = new ProjectDTO
        {
            Name = "Titulo",
            Description = "Descripcion",
            
        };
        var _context = _contextFactory.Generate();
        _context.Project.Add(_project.FromEntity().Item1);
        _context.SaveChanges();
        
        return;
        Expression<Func<ProjectTask, bool>> UniqueNameRule(ProjectTask task) => p => p.Title == task.Title && p.ProjectId == task.ProjectId && p.Id != task.Id;
        int GetKeyValue(ProjectTask project) => project.Id;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void AddTask_AddTaskCorrectly()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        var result = _controller.Add(taskDto);
        
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void AddTaskWithTrackNameInSameProject_ReturnError()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        var result = _controller.Add(taskDto);
        
        Assert.IsInstanceOfType<Error>(result);
        
    }
    
    [TestMethod]
    public void AddTaskWithTrackNameInDifferentProject_AddTaskCorrectly()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);

        var task = taskDto.FromEntity().Item1!;
        task.ProjectId = project.Id;
        
        _controller.Add(TaskDTO.ToEntity(task));
        
        _project.Name = "Titulo2";
        _context.Project.Add(_project.FromEntity().Item1!);
        _context.SaveChanges();
        
        project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        var taskTest = taskDto.FromEntity().Item1;
        taskTest.ProjectId = project.Id;
        taskDto = TaskDTO.ToEntity(taskTest);
        
        var result = _controller.Add(taskDto);
        
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void FindBy_ReturnTask()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var result = _controller.FindBy(p => p.Title == taskDto.Title);
    
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void FindByTaskNonTrackName_ReturnNull()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        var result = _controller.FindBy(p => p.Title == taskDto.Title);
    
        Assert.IsNull(result);
    }
    
    
    [TestMethod]
    public void DeleteTaskTrackName_ReturnTrue()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        var result = _controller.Add(taskDto);
        
        Assert.IsNull(result);
        
        var taskAux = _controller.FindBy(p => p.Id == taskDto.FromEntity().Item1.Id);
        if (taskAux == null) return;
        
        var deleted = _controller.Remove(taskAux.Id);
    
        Assert.IsTrue(deleted);
    }
    
    [TestMethod]
    public void GetById_ReturnTask()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var result = _controller.GetById(1);
        
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetById_ReturnNull()
    {
        var result = _controller.GetById(1);
        
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void GetAllWithEmptyTask_ReturnNull()
    {
        var result = _controller.GetAll();
        
        Assert.IsTrue(result.IsNullOrEmpty());
    }
    
    [TestMethod]
    public void GetAllWithTask_ReturnTask()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var result = _controller.GetAll();
        
        Assert.IsTrue(result.Count == 1);
    }
    
    [TestMethod]
    public void DeleteTaskNonTrackName_ReturnFalse()
    {
        var deleted = _controller.Remove(2);
    
        Assert.IsFalse(deleted);
    }
    
    [TestMethod]
    public void UpdateWithNonTrackTask_ReturnError()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var result = _controller.Update(taskDto);
        
        Assert.IsInstanceOfType<Error>(result);
    }
    
    [TestMethod]
    public void UpdateTask_UpdateCorrectly()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var taskAux = _controller.FindBy(p =>  p.Title == taskDto.Title && p.ProjectId == taskDto.ProjectId);
        
        taskAux.Title = "pepinillo";
        _ = _controller.Update(TaskDTO.ToEntity(taskAux));
    
        var updatedProject = _controller.FindBy(p => p.Title == taskAux.Title && p.ProjectId == taskAux.ProjectId);
        Assert.AreNotEqual(project.Name, updatedProject!.Title);
    }
    
    [TestMethod]
    public void ExistWithTrackTask_ReturnTrue()
    {
        var _context = _contextFactory.Generate();
        var taskDto = new TaskDTO();
        taskDto.Title = "Titulo";
        taskDto.Description = "Descripcion";
        taskDto.DurationHour = 20;
        
        var project = _context.Project.FirstOrDefault(p => p.Name == _project.Name);
        
        taskDto.FromEntity().Item1.ProjectId = project.Id;
        
        _controller.Add(taskDto);
        
        var result = _controller.Exists(p => p.Title == taskDto.Title && p.ProjectId == taskDto.ProjectId);
        
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void ExistWithNonTrackTask_ReturnTrue()
    {
        var result = _controller.Exists(p => p.Id == 3);
        
        Assert.IsFalse(result);
    }
}