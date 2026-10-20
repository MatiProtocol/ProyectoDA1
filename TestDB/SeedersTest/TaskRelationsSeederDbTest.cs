using DBContext;
using Domain;
using Services.SeederDB;

namespace TestDB.SeedersTest;
[TestClass]
public class TaskRelationsDbSeederTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;
    [TestInitialize]
    public void Initialize()
    {
        _contextFactory = new InMemorySqlContextFactory("TaskRelationsDbSeederTests" + _i);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void SeedWhenProjectsExistAndNoTasks_AddTasks()
    {
        var context = _contextFactory.Generate();
        var project = new Project("Project1", "Description");
        context.Project.Add(project);
        context.SaveChanges();
        
        TaskRelationsDbSeeder.Seed(_contextFactory);
        
        var tasks = context.ProjectTask.ToList();

        Assert.AreEqual(2, tasks.Count);
    }
    
    [TestMethod]
    public void SeedWhenProjectsExistAndNoTasks_AddRelations()
    {
        var context = _contextFactory.Generate();
        
        var project = new Project("Project1", "Description");
        context.Project.Add(project);
        context.SaveChanges();
        
        TaskRelationsDbSeeder.Seed(_contextFactory);
        
        var relations = context.TaskToTask.ToList();
        Assert.AreEqual(1, relations.Count);
    }

    [TestMethod]
    public void SeedWhenProjectsExistAndNoTasks_RelationsAreTaskAToTaskB()
    {
        var context = _contextFactory.Generate();
       
        var project = new Project("Project1", "Description");
        context.Project.Add(project);
        context.SaveChanges();
        
        TaskRelationsDbSeeder.Seed(_contextFactory);
        var relations = context.TaskToTask.ToList();

        var relation = relations.First();
        
        var result = relation.ProjectTaskId == 1 && relation.DependentTaskId == 2;
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void SeedWhenTasksAlreadyExist_NotAddTasks()
    {
        var context = _contextFactory.Generate();
        
        var project = new Project("Project1", "Description");
        context.Project.Add(project);
        context.ProjectTask.Add(new ProjectTask("ExistingTask", "desc", 1));
        context.SaveChanges();
        
        TaskRelationsDbSeeder.Seed(_contextFactory);
        
        Assert.AreEqual(1, context.ProjectTask.Count());
    }
    
    [TestMethod]
    public void SeedWhenTasksAlreadyExist_NotAddTasksRelations()
    {
        var context = _contextFactory.Generate();
        
        var project = new Project("Project1", "Description");
        context.Project.Add(project);
        context.ProjectTask.Add(new ProjectTask("ExistingTask", "desc", 1));
        context.SaveChanges();
        
        TaskRelationsDbSeeder.Seed(_contextFactory);
        Assert.AreEqual(0, context.TaskToTask.Count());
    }

    [TestMethod]
    public void Seed_ShouldNotAddTasks_WhenNoProjectsExist()
    {
        var context = _contextFactory.Generate();
        TaskRelationsDbSeeder.Seed(_contextFactory);
        
        Assert.AreEqual(0, context.ProjectTask.Count());
        Assert.AreEqual(0, context.TaskToTask.Count());
    }
}

