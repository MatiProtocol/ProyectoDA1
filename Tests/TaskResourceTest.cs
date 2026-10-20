using Domain.HelpersDB;

namespace Tests;

[TestClass]
public class TaskResourceTest
{
    
    [TestMethod]
    public void CreateNewTaskResource_NotNull()
    {
        var result = new TaskResource();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetSetCount_NotNull()
    {
        var projectResourceExclusive = new TaskResource
        {
            Count = 1
        };

        var result = projectResourceExclusive.Count;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetResourceId_NotNull()
    {
        var projectResourceExclusive = new TaskResource
        {
            ResourceId = 1
        };

        var result = projectResourceExclusive.ResourceId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetProjectTaskId_NotNull()
    {
        var projectResourceExclusive = new TaskResource
        {
            ProjectTaskId = 1
        };

        var result = projectResourceExclusive.ProjectTaskId;
        
        Assert.IsTrue(result == 1);
    }
    
}