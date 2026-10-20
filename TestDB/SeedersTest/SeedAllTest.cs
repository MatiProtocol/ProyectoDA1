using DBContext;
using Services.SeederDB;

namespace TestDB.SeedersTest;

[TestClass]
public class SeedAllTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;

    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory =new  InMemorySqlContextFactory("SeedAllTests" + _i);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void SeedWhenDatabaseIsEmpty_ShouldInsertData()
    {
        var context = _contextFactory.Generate();
        
        SeedAll.Seed(_contextFactory);
        
        var result =
            context.Users.Any() &&
            context.Project.Any() &&
            context.Resource.Any() &&
            context.Notification.Any() &&
            context.TaskToTask.Any();

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SeedWhenCalledMultipleTimes_NotDuplicateData_()
    {
        var context = _contextFactory.Generate();
        
        SeedAll.Seed(_contextFactory);
        SeedAll.Seed(_contextFactory);
        
        var result = 
            context.Users.Count() == 18 &&
            context.Project.Count() == 11 &&
            context.Resource.Count() == 6 &&
            context.Notification.Count() == 1 &&
            context.TaskToTask.Count() == 11;
        
        Assert.IsTrue(result);
    }
}