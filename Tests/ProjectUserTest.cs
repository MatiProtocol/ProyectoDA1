
using Auxiliaries;
using Domain.HelpersDB;

namespace Tests;

[TestClass]
public class ProjectUserTest
{

    [TestMethod]
    public void CreateNewProjectUserTestDB_NotNull()
    {
        var result = new ProjectUser();
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetSetProjectId_NotNull()
    {
        var projectResourceExclusive = new ProjectUser
        {
            ProjectId = 1
        };

        var result = projectResourceExclusive.ProjectId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetUserId_NotNull()
    {
        var projectResourceExclusive = new ProjectUser
        {
            UserId = 1
        };

        var result = projectResourceExclusive.UserId;
        
        Assert.IsTrue(result == 1);
    }
    
    [TestMethod]
    public void GetSetRole_NotNull()
    {
        var projectResourceExclusive = new ProjectUser
        {
            Role = Roles.ProjectLeader
        };

        var result = projectResourceExclusive.Role;
        
        Assert.IsTrue(result == Roles.ProjectLeader);
    }

}