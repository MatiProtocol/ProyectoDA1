using DBContext;
using Domain;
using Services.SeederDB;

namespace TestDB.SeedersTest;

[TestClass]
public class ResourceSeederDbTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;
    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("ResourceSeederDbTests" + _i);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void SeedWhenDatabaseIsEmpty_InsertResources()
    {
        var context = _contextFactory.Generate();
        ResourceSeederDb.Seed(_contextFactory);

        var resources = context.Resource.ToList();
        Assert.AreEqual(6, resources.Count);
    }

    [TestMethod]
    public void SeedWhenResourcesAlreadyExist_ShouldNotInsertResources()
    {
        var context = _contextFactory.Generate();
        context.Resource.Add(new Resource("Existing", "Type", "Description", 1));
        context.SaveChanges();

        ResourceSeederDb.Seed(_contextFactory);

        var resources = context.Resource.ToList();
        Assert.AreEqual(1, resources.Count);
    }
}