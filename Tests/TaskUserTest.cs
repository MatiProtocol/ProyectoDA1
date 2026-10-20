using Domain.HelpersDB;

namespace Tests;

[TestClass]
public class TaskUserTest
{
    [TestMethod]
    public void CreateNewTaskUser_NotNull()
    {
        var result = new TaskUser();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetSetProjectTaskId_NotNull()
    {
        var projectResourceExclusive = new TaskUser
        {
            ProjectTaskId = 1
        };

        var result = projectResourceExclusive.ProjectTaskId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetUserId_NotNull()
    {
        var projectResourceExclusive = new TaskUser
        {
            UserId = 1
        };

        var result = projectResourceExclusive.UserId;
        
        Assert.IsTrue(result == 1);
    }
}