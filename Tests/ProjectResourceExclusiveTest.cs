using Domain.HelpersDB;

namespace Tests;


[TestClass]
public class ProjectResourceExclusiveTest
{

    [TestMethod]
    public void CreateNewProjectResourceExclusive_NotNull()
    {
        var result = new ProjectResourceExclusive();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetSetResourceId_NotNull()
    {
        var projectResourceExclusive = new ProjectResourceExclusive
        {
            ResourceId = 1
        };

        var result = projectResourceExclusive.ResourceId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetProjectId_NotNull()
    {
        var projectResourceExclusive = new ProjectResourceExclusive
        {
            ProjectId = 1
        };

        var result = projectResourceExclusive.ProjectId;
        
        Assert.IsTrue(result == 1);
    }

}