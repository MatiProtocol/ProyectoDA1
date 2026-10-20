using DBContext;
using Domain;
using Services.SeederDB;

namespace TestDB.SeedersTest;

[TestClass]
public class ProjectSeederTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;
    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("ProjectSeederTests" + _i);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void SeedWhenDatabaseIsEmpty_Insert10Projects_()
    {
        var context = _contextFactory.Generate();
        ProjectSeederDb.Seed(_contextFactory);

        var projects = context.Project.ToList();
        Assert.AreEqual(11, projects.Count);
    }

    [TestMethod]
    public void SeedWhenProjectsAlreadyExist_NotInsertProjects()
    {
        var context = _contextFactory.Generate();
        context.Project.Add(new Project("Existing Project", "Test Description"));
        context.SaveChanges();

        ProjectSeederDb.Seed(_contextFactory);

        var projects = context.Project.ToList();
        Assert.AreEqual(1, projects.Count);
    }
}