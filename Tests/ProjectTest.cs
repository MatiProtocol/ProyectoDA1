using System.Globalization;
using Domain;
using Auxiliaries;

namespace Tests;

[TestClass]
public class ProjectTest
{
    private Project _project = null!;

    [TestInitialize]
    public void Initialize()
    {
        _project = new Project("Proje", "Descripcion");
    }

    [TestMethod]
    public void CreateNewProject_NotNull()
    {
        Assert.IsNotNull(_project);
    }
    
    [TestMethod]
    public void CreateNewProjectWithEmptyConstructor_NotNull()
    {
        _project = new Project();
        Assert.IsNotNull(_project);
    }

    [TestMethod]
    public void NameGetSet_AssignCorrectly()
    {
        _project.Name = "LaDivinaComedia";
        
        Assert.AreEqual("LaDivinaComedia", _project.Name);
    }
    
    [TestMethod]
    public void GetSetId_ReturnId()
    {
        _project.Id = 1;
        var result = _project.Id;
        Assert.AreEqual(1, result);
    }
    
    [TestMethod]
    public void DescriptionGetSet_AssignCorrectly()
    {
        var desc = "El poeta inicia su camino, perdido en tinieblas. Encuentra a Virgilio, quien promete acompañarle.";
        _project.Description = desc;
        
        Assert.AreEqual(desc, _project.Description);
    }

    [TestMethod]
    public void StartDateGetSet_AssignCorrectly()
    {
        DateTime date = DateTime.Parse("10/05/2025");
        
        _project.StartDate = date;
        
        Assert.AreEqual(date, _project.StartDate);
    }
    
    [TestMethod]
    public void EndDateGetSet_AssignCorrectly()
    {
        var date = DateTime.Parse("10/05/2025");
        
        _project.EndDate = date;
        
        Assert.AreEqual(date, _project.EndDate);
    }

    [TestMethod]
    public void AddTaskToList_GetTheTaskFromList()
    {
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        
        _project.AddTaskToList(task);
        var retrievedTask = _project.GetTask("Titulo");
        
        Assert.AreEqual(task, retrievedTask);
    }

    [TestMethod]
    public void GetTeam_NotNull()
    {
        Assert.IsNotNull(_project.Team);
    }

    [TestMethod]
    public void SetALongDescription_ThrowError()
    {
        string longDesc = new string('*', 401);
        
        try
        {
            _project.Description = longDesc;
        }
        catch (ArgumentException e)
        {
            Assert.AreEqual(e.Message, "The description must not longer than 400 characters.");
        }
    }

    [TestMethod]
    public void LongDescriptionInConstructor_ExpectError()
    {
        string longDesc = new string('a', 401);
        
        Assert.ThrowsException<ArgumentException>(() => new Project("Proje", longDesc));
    }

    [TestMethod]
    public void SetGetTaskGraph_IsNotNull()
    {
        _project.TaskGraph = new TaskGraph();
        
        Assert.IsNotNull(_project.TaskGraph);
    }

    [TestMethod]
    public void GetSetTaskList_IsNotNull()
    {
        _project.TaskList = [];
        
        Assert.IsNotNull(_project.TaskList);
    }
    
    [TestMethod]
    public void GetSetTaskToUser_IsNotNull()
    {
        _project.TaskToUser = [];
        
        Assert.IsNotNull(_project.TaskToUser);
    }

    [TestMethod]
    public void GetSetUserToTask_IsNotNull()
    {
        _project.UserToTask = [];
        
        Assert.IsNotNull(_project.UserToTask);
    }
    
    [TestMethod]
    public void GetSetUserDelay_NotNUll()
    {
        _project.Delay = DateTime.Today; 
        Assert.AreEqual(DateTime.Today, _project.Delay);
    }

    [TestMethod]
    public void SetTeamValidateManager_SetCorrectlyAndIsCorrectManager()
    {
        var team = new ProjectTeam();
        byte[] pass = [0, 2];
        var manager = new User("lolo",
            "fibonacci",
            "lolo@gmail.com",
            DateTime.Parse("01/01/2001",
                CultureInfo.CreateSpecificCulture("en-US")),
            pass);
        manager.Role = Roles.SystemManager;
        team.Manager = manager;
        
        _project.Team = team;
        var result = _project.ValidateProjectManager(manager.Email);
        
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SetTeamValidateManager_SetCorrectlyAndIsIncorrectManager()
    {
        var team = new ProjectTeam();
        byte[] pass = [0, 2];
        var correctManager = new User("Gonza", "Bacardí", "gonza@gmail.com", DateTime.Parse("01/01/2001",CultureInfo.CreateSpecificCulture("en-US")), pass);
        var otherUser = new User("lolo", "fibonacci", "lolo@gmail.com", DateTime.Parse("01/01/2001",CultureInfo.CreateSpecificCulture("en-US")), pass);
        correctManager.Role = Roles.SystemManager;
        otherUser.Role = Roles.SystemManager;
        team.Manager = correctManager;
        
        _project.Team = team;
        var result = _project.ValidateProjectManager(otherUser.Email);
        
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void SetGetResourceRelations_AssignCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 3);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var relation = new Dictionary<string, List<(Resource, int)>>();
        var resourceRelation = new List<(Resource, int)> {
            (resource, 1) };

        relation.Add("Titulo", resourceRelation);
        _project.ResourceRelation = relation; 

        Assert.AreEqual(relation, _project.ResourceRelation);
    }

    [TestMethod]
    public void SetGetResourceExclusive_AssignCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        
        resource.ToggleExclusiveness();
        exclusive.Add((resource, 1));
        _project.ResourceExclusive = exclusive;
        
        Assert.AreEqual(exclusive, _project.ResourceExclusive);
    }

    [TestMethod]
    public void AddResourceRelations_AssignCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var relation = new Dictionary<string, List<(Resource, int)>>();
        var resourceRelation = new List<(Resource, int)>();
        
        resourceRelation.Add((resource, 1));
        relation.Add("Titulo", resourceRelation);
        _project.AddResourceRelation("Titulo", resource, 1);
        
        Assert.AreEqual(relation.Count, _project.ResourceRelation.Count);
    }
    
    [TestMethod]
    public void AddResourceRelations_UpdateResourceWithTask()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var relation = new Dictionary<string, List<(Resource, int)>>();
        var resourceRelation = new List<(Resource, int)>();
        
        resourceRelation.Add((resource, 1));
        relation.Add("Titulo", resourceRelation);
        _project.AddResourceRelation("Titulo", resource, 1);
        
        Assert.AreEqual(1,  resource.Queue.Count);
    }

    [TestMethod]
    public void AddResourceRelationsTwiceSameTask_AssignCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var resource2 = new Resource("asasa", "rrrr", "eeee", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var relation = new Dictionary<string, List<(Resource, int)>>();
        var resourceRelation = new List<(Resource, int)>();
        
        resourceRelation.Add((resource, 1));
        resourceRelation.Add((resource2, 1));
        relation.Add("Titulo", resourceRelation);
        _project.AddResourceRelation("Titulo", resource, 1);
        _project.AddResourceRelation("Titulo", resource2, 1);
        
        Assert.AreEqual(relation.Count, _project.ResourceRelation.Count);
    }

    [TestMethod]
    public void AddResourceRelationsNonTrackedTask_NotAssigned()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Ticholo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var resourceRelationDic = new Dictionary<string, List<(Resource, int)>>();
        var resourceList = new List<(Resource, int)>();
        
        resourceList.Add((resource, 1));
        resourceRelationDic.Add("Ticholo", resourceList);
        _project.AddResourceRelation("Ticholo", resource, 1);
        _project.AddResourceRelation("Ticholo", resource, 1);
        
        Assert.AreEqual(resourceRelationDic.Count, _project.ResourceRelation.Count);
    }

    [TestMethod]
    public void AddResourceRelationsBigQuantity_DoesNotAdd()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Ticholo", "Descripcion", 5);
        _project.AddTaskToList(task);
        
        _project.AddResourceRelation("Ticholo", resource, 20);
        Assert.AreEqual(0, _project.ResourceRelation.Count);
    }

    [TestMethod]
    public void AddResourceExclusive_AddCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        
        resource.ToggleExclusiveness();
        exclusive.Add((resource, 1));
        _project.AddResourceExclusive(resource, 1);
        
        Assert.AreEqual(1, _project.ResourceExclusive.Count);
    }

    [TestMethod]
    public void AddResourceExclusiveNotExclusiveYet_AddCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        

        exclusive.Add((resource, 1));
        _project.AddResourceExclusive(resource, 1);
        
        Assert.AreEqual(1, _project.ResourceExclusive.Count);
    }
    
    [TestMethod]
    public void AddResourceExclusiveToProjectBiggerQuantity_DoesNotAdd()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        

        exclusive.Add((resource, 1));
        _project.AddResourceExclusive(resource, 3);
        
        Assert.AreEqual(0, _project.ResourceExclusive.Count);
    }

    [TestMethod]
    public void AddResourceExclusiveTwice_AddCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var resource2 = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        var exclusive2 = new List<(Resource, int)>();
        
        resource.ToggleExclusiveness();
        resource2.ToggleExclusiveness();
        exclusive.Add((resource, 1));
        exclusive2.Add((resource2, 1));
        _project.AddResourceExclusive(resource, 1);
        _project.AddResourceExclusive(resource2, 1);
        
        Assert.AreEqual(1, _project.ResourceExclusive.Count);
    }

    [TestMethod]
    public void RemoveResourceExclusive_RemoveCorrectly()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        
        resource.ToggleExclusiveness();
        exclusive.Add((resource, 1));
        _project.AddResourceExclusive(resource, 1);
        _project.RemoveResourceExclusive(resource);
        
        Assert.AreEqual(0, _project.ResourceExclusive.Count);
    }

    [TestMethod]
    public void RemoveResourceExclusiveNotTracked_DoesNotRemove()
    {
        var resource = new Resource("asdasd", "erer", "aszxc", 1);
        var resourceRemove = new Resource("seleste", "loool", "aszxc", 1);
        var task = new ProjectTask("Titulo", "Descripcion", 5);
        _project.AddTaskToList(task);
        var exclusive = new List<(Resource, int)>();
        
        resource.ToggleExclusiveness();
        exclusive.Add((resource, 1));
        _project.AddResourceExclusive(resource, 1);

        _project.RemoveResourceExclusive(resourceRemove);
        
        Assert.AreEqual(1, _project.ResourceExclusive.Count);
    }

    [TestMethod]
    public void AssignMemberToTask()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);

        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);

        _project.AddTaskToList(task);

        _project.AssignedUserToTask(task, user);

        Assert.AreEqual((1, 1), (_project.GetUserAssignedToTask(task).Length, _project.GetTaskAssignedToUser(user).Length));
    }

    [TestMethod]
    public void AssignMemberToTask_SameUserTwice()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);

        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);

        _project.AddTaskToList(task);

        _project.AssignedUserToTask(task, user);

        _project.AssignedUserToTask(task, user);

        Assert.AreEqual((1, 1), (_project.GetUserAssignedToTask(task).Length, _project.GetTaskAssignedToUser(user).Length));
    }

    [TestMethod]
    public void UnassignMemberToTask_WhenTaskWorks()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);

        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);

        _project.AddTaskToList(task);

        _project.AssignedUserToTask(task, user);
        
        _project.UnassignUserToTask(task, user);


        Assert.AreEqual((0, 0), (_project.GetUserAssignedToTask(task).Length, _project.GetTaskAssignedToUser(user).Length));
    }
    
    [TestMethod]
    public void UnassignMemberToTask_WhenTaskIsNotAssigned()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);
        
        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _project.AddTaskToList(task);
        _project.AddTaskToList(task1);

        _project.AssignedUserToTask(task, user);

        var result = _project.UnassignUserToTask(task1, user);

        Assert.IsFalse(result);
    }
    
    [TestMethod]
    public void UnassignMemberToTask_WhenUserIsNotAssigned()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());
        var user1 = new User("peo", "pep", "peq@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);
        team.AddMember(user1);
        
        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        
        _project.AddTaskToList(task);

        _project.AssignedUserToTask(task, user);

        var result = _project.UnassignUserToTask(task, user1);

        Assert.IsFalse(result);
    }
    
    [TestMethod]
    public void UnassignMemberToTask_WhenUserHasEntryButNotWithThatTask()
    {
        var user = new User("peo", "pep", "pe@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());
        var user1 = new User("peo", "pep", "pe1j@pe", DateTime.Today.AddYears(-18), "\0\0"u8.ToArray());

        var team = new ProjectTeam();
        team.AddMember(user);
        team.AddMember(user1);

        _project.Team = team;

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 10);
        
        _project.AddTaskToList(task);
        _project.AddTaskToList(task1);

        _project.AssignedUserToTask(task, user1);
        _project.AssignedUserToTask(task1, user);
        
        var result = _project.UnassignUserToTask(task, user);


        Assert.IsFalse(result);
    }
    
    
}
