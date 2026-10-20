using Domain.HelpersDB;

namespace Tests;

[TestClass]
public class TaskToTaskTest
{
    [TestMethod]
    public void CreateNewTaskToTask_NotNull()
    {
        var result = new TaskToTask();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetSetProjectTaskId_NotNull()
    {
        var projectResourceExclusive = new TaskToTask
        {
            ProjectTaskId = 1
        };

        var result = projectResourceExclusive.ProjectTaskId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetDependentTaskId_NotNull()
    {
        var projectResourceExclusive = new TaskToTask
        {
            DependentTaskId = 1
        };

        var result = projectResourceExclusive.DependentTaskId;
        
        Assert.IsTrue(result == 1);
    }
}