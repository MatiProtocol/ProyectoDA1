using DataAccess;
using DBContext;
using Domain;

namespace Tests;

[TestClass]
public class PureDatabaseTest
{

    private ISqlContextFactory _contextFactory;
    private int _i = 0;
    private User user; 
    
    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("PureDatabaseTest" + _i);
        user = new User("Petro", "Pepe", "pepp@ajasdk", DateTime.Now.AddYears(-20), [1, 3, 3, 8, 4]);
        PureDatabase.GetInstance(_contextFactory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void CreateDataBase_IsNotNull()
    {
        var database = PureDatabase.GetInstance();
        Assert.IsNotNull(database);
    }

    [TestMethod]
    public void UserRepository_IsNotNull()
    {
        var database = PureDatabase.GetInstance();

        var result = database.Users;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ProjectRepository_IsNotNull()
    {
        var database = PureDatabase.GetInstance();

        var result = database.Projects;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ResourceRepository_IsNotNull()
    {
        var database = PureDatabase.GetInstance();

        var result = database.Resources;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MemoryDataBaseReset_ClearAllData_User()
    {
        var database = PureDatabase.GetInstance();
        database.Notifications.Add(new Notification(user, "BROWSER"));
        database.Projects.Add(new Project("pe", "pe"));
        database.Resources.Add(new Resource("bot","botin","asd",1));
        database.Users.Add(user);

        database.Reset();

        Assert.AreEqual(0, database.Users.GetAll().Count);
    }
    
    [TestMethod]
    public void MemoryDataBaseReset_ClearAllData_Resource()
    {
        var database = PureDatabase.GetInstance();
        database.Notifications.Add(new Notification(user, "FERNANDO ALOSNO"));
        database.Projects.Add(new Project("pe", "pe"));
        database.Resources.Add(new Resource("bot","botin","asd",1));
        database.Users.Add(user);

        database.Reset();

        Assert.AreEqual(0, database.Resources.GetAll().Count);
    }
    
    [TestMethod]
    public void MemoryDataBaseReset_ClearAllData_Project()
    {
        var database = PureDatabase.GetInstance();
        database.Projects.Add(new Project("pe", "pe"));
        database.Resources.Add(new Resource("bot","botin","asd",1));
        database.Users.Add(user);

        database.Reset();

        Assert.AreEqual(0, database.Projects.GetAll().Count);
    }
    
    [TestMethod]
    public void MemoryDataBaseReset_ClearAllData_Notification()
    {
        var database = PureDatabase.GetInstance();
        database.Projects.Add(new Project("pe", "pe"));
        database.Resources.Add(new Resource("bot","botin","asd",1));
        
        database.Users.Add(user);
        database.Notifications.Add(new Notification(user, "Merci BAGUETTE"));

        database.Reset();

        Assert.AreEqual(0, database.Notifications.Query().Count());
    }
}