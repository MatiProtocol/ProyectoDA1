using Auxiliaries;
using DBContext;
using Domain;
using Services.SeederDB;

namespace TestDB.SeedersTest;

[TestClass]
public class UserSeederDbTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;

    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("UserSeederDbTests" + _i);
    }

    [TestCleanup]
    public void Cleanupo()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void SeedUserSeeder_InsertAllUsers()
    {
        var context = _contextFactory.Generate();
        UserSeederDb.Seed(_contextFactory);

        var users = context.Users.ToList();
        Assert.AreEqual(18, users.Count);
    }

    [TestMethod]
    public void Seed_ShouldNotInsertUsers_WhenDatabaseIsNotEmpty()
    {
        var context = _contextFactory.Generate();
        context.Users.Add(new User("Test", "User", "test@example.com", new DateTime(2000, 1, 1), new byte[32]));
        context.SaveChanges();

        UserSeederDb.Seed(_contextFactory);

        var users = context.Users.ToList();
        Assert.AreEqual(1, users.Count);
    }

    [TestMethod]
    public void SeedUserSeeder_RootAreSystemManager()
    {
        var context = _contextFactory.Generate();
        UserSeederDb.Seed(_contextFactory);

        var users = context.Users.ToList();
        var root = users.FirstOrDefault(u => u.Email == "root@root.com");

        Assert.IsNotNull(root);
        Assert.AreEqual(Roles.SystemManager, root.Role);
    }
    
    [TestMethod]
    public void SeedUserSeeder_HaveTreeProjectLeaderManager()
    {
        var context = _contextFactory.Generate();
        
        UserSeederDb.Seed(_contextFactory);

        var users = context.Users.ToList();

        var projectLeaders = users.Where(u => u.Role == Roles.ProjectLeader).ToList();
        Assert.AreEqual(4, projectLeaders.Count);
    }
}