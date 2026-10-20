using Auxiliaries;
using Domain;

namespace Tests;

[TestClass]
public class ProjectTeamTest
{
    private ProjectTeam _projectTeam = null!;
    private User _user = null!;

    [TestInitialize]
    public void Initialize()
    {
        byte[] bytePass = [1, 2, 3];
        _projectTeam = new ProjectTeam();
        _user = new User("Thiago", "Espinosa", "thiago6@gmail.com", DateTime.Parse("10/04/2004"), bytePass);
    }


    [TestMethod]
    public void CreateNewProjectTeam_NotNull()
    {
        Assert.IsNotNull(_projectTeam);
    }

    [TestMethod]
    public void GetSetMembers_ReceiveMembersSet()
    {
        _projectTeam.Members = new Dictionary<string, User>();
        
        var members = _projectTeam.Members;

        Assert.IsNotNull(members);
    }

    [TestMethod]
    public void GetSetManager_ReceiveManager()
    {
        _projectTeam.Manager = _user;
        var manager = _projectTeam.Manager;
        
        Assert.AreEqual(_user, manager);
    }

    [TestMethod]
    public void AddGetAMember_ReceiveMemberFromHashMap()
    {
        _projectTeam.AddMember(_user);
        var retrieved = _projectTeam.GetAMember(_user);
        
        Assert.AreEqual(_user, retrieved);
    }

    [TestMethod]
    public void AddGetLeader_AssignCorrectlyLeader()
    {
        _user.Role = Roles.ProjectLeader;
        _projectTeam.Leader = _user;

        Assert.AreEqual(_user.Email, _projectTeam.Leader.Email);
    }

    [TestMethod]
    public void GetSetId_ReturnId()
    {
        _projectTeam.Id = 1;
        var id = _projectTeam.Id;
    }

}