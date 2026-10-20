using Auxiliaries;
using Domain;
using Services.DTOs;

namespace Tests;

[TestClass]
public class TaskDtoTest
{
    [TestMethod]
    public void ToEntity_ConvertCorrectly()
    {
        var task = new ProjectTask("piripichi", "parangutirimiculo", 10);
        
        var result = TaskDTO.ToEntity(task);
        
        Assert.IsInstanceOfType<TaskDTO>(result);
    }

    [TestMethod]
    public void FromEntity_ConvertCorrectly()
    {
        var taskDto = new TaskDTO
        {
            Title = "piripichi",
            Description = "parangutirimiculo",
            DurationHour = 10
        };
        
        var task = taskDto.FromEntity().Item1;
        
        Assert.IsInstanceOfType<ProjectTask>(task);
    }
    
    [TestMethod]
    public void FromEntity_ConvertIncorrectly()
    {
        var taskDto = new TaskDTO
        {
            Title = "",
            Description = "parangutirimiculo",
            DurationHour = 10
        };
        
        var error = taskDto.FromEntity().Item2;
        
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void GetId_ReturnInt()
    {
        var task = new ProjectTask("piripichi", "parangutirimiculo", 10);
        
        var result = TaskDTO.ToEntity(task);
        
        Assert.IsTrue(result.Id == 0);
    }
}