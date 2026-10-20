using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using DomainConstruct;
using Services;
using Services.DTOs;

namespace Tests;

[TestClass]
public class TaskGraphServiceTest
{

    private ISqlContextFactory _contextFactory;
    private static int _i = 0;

    private UserDTO _systemManager;
    private UserDTO _user;

    private PureDatabase _db;
    private Project _project = new Project("Pepe", "Desc");
    private ProjectDTO _projectDto;
    private TaskGraphService _taskGraphService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        if(DateTime.Now.Second == 59) Thread.Sleep(2000);
        
        _i += 1;
        _contextFactory = new InMemorySqlContextFactory("TaskGraphServiceTest" + _i);
        _db = PureDatabase.GetNewInstance(_contextFactory);
        
        _systemManager = new UserDTO
        {
            Name = "System",
            LastName = "Manager",
            Email = "test2@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.SystemManager
        };
        
        _user = new UserDTO
        {
            Name = "System1",
            LastName = "Manager1",
            Email = "test3@test.com",
            BirthDate = "04/18/2003",
            Password = "123456Ac@",
            Role = Roles.SystemManager
        };

        var context = _contextFactory.Generate();
        
        var systemManager = _systemManager.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        _db.Users.Add(systemManager);
        _systemManager = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _systemManager.Email)!);
        
        var user = _user.FromEntity(UserService.EncryptPassword("123456Ac@")).Item1!;
        _db.Users.Add(user);
        _user = UserDTO.ToEntity(_db.Users.FindBy(u => u.Email == _user.Email)!);
        
        var projS = ProjectService.GetNewInstance(_contextFactory);
        projS.CreateProject(_systemManager, ProjectDTO.ToEntity(_project));
        _project = _db.Projects.FindBy(p => p.Name == _project.Name)!;
        _projectDto = projS.GetProjectDto(_project.Name).Item1!;

        _taskGraphService = new TaskGraphService(_project, _contextFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }


    [TestMethod]
    public void AddTaskToTaskGraph_TaskIsOnMap()
    {
        _taskGraphService.Add(new ProjectTask("Titulo", "Descripcion", 5));
        Assert.IsTrue(_taskGraphService.Has(new ProjectTask("Titulo", "Descripcion", 5)));
    }

    [TestMethod]
    public void AddManyTaskToTaskGraph_TasksIsOnMap()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);
        _taskGraphService.AddMany([a, b]);
        Assert.IsTrue(_taskGraphService.HasMany([a, b]));
    }

    [TestMethod]
    public void EmptyTaskGraph_TaskIsNotMap()
    {
        _ = new TaskGraph();
        Assert.IsFalse(_taskGraphService.Has(new ProjectTask("Titulo", "Descripcion", 5)));
    }

    [TestMethod]
    public void RemoveTaskOnTaskGraph_TaskIsNotInMap()
    {
        _taskGraphService.Add(new ProjectTask("Titulo", "Descripcion", 5));
        _taskGraphService.Remove(new ProjectTask("Titulo", "Descripcion", 5));
        Assert.IsFalse(_taskGraphService.Has(new ProjectTask("Titulo", "Descripcion", 5)));
    }

    [TestMethod]
    public void RemoveTaskWhichNotExist_MustReturnFalse() =>
        Assert.IsFalse(_taskGraphService.Remove(new ProjectTask("Titulo", "Descripcion", 5)));

    [TestMethod]
    public void RemoveTaskOnTaskGraph_TasksIsNotInMap()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(c, [a, b], TypeDependency.FS);

        _taskGraphService.RemoveMany([a, b]);

        Assert.IsFalse(_taskGraphService.HasMany([a, b]));
    }

    [TestMethod]
    public void AddDependencyToTask_aMustDependOnb()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        Assert.AreEqual((TypeDependency.FS, b), _taskGraphService.GetTaskDependencies(a)[0]);
    }

    [TestMethod]
    public void AddDependencyToTask_aMustDependOnbc()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);
        var c = new ProjectTask("c", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        CollectionAssert.AreEqual(new (TypeDependency, ProjectTask)[] { (TypeDependency.FS, b), (TypeDependency.FS, c) }, _taskGraphService.GetTaskDependencies(a));
    }
    
    [TestMethod]
    public void AddDependencyToTaskDuplicateDependency_ReturnError()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        Assert.IsInstanceOfType<Error?>(_taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS));
    }
    
    [TestMethod]
    public void AddDependencyToTaskWithNonTrackTask_ReturnError()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);

        _taskGraphService.Add(a);
        
        Assert.IsInstanceOfType<Error?>(_taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS));
    }
    
    [TestMethod]
    public void AddDependencyToTaskWithCycle_ReturnError()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        Assert.IsInstanceOfType<Error?>(_taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS));
    }

    [TestMethod]
    public void RemoveTaskOnTaskGraph_ItShouldNotBeInAnyDependency()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        _taskGraphService.AddDependencyToTask(b, c, TypeDependency.FS);

        _taskGraphService.Remove(b);

        Assert.AreEqual(0, _taskGraphService.GetTaskDependencies(a).Length);
    }

    [TestMethod]
    public void RemoveDependenciesToTask_aMustNotDependOnbButOnC()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);
        var c = new ProjectTask("c", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        _taskGraphService.RemoveDependencyFromTask(a, b);

        Assert.AreEqual((TypeDependency.FS, c), _taskGraphService.GetTaskDependencies(a)[0]);
    }

    [TestMethod]
    public void RemoveDependenciesToTask_aMustNotDependOnbc()
    {
        var a = new ProjectTask("a", "", 1);
        var b = new ProjectTask("b", "", 1);
        var c = new ProjectTask("c", "", 1);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        _taskGraphService.RemoveDependencyFromTaskMany(a, [b, c]);

        Assert.AreEqual(0, _taskGraphService.GetTaskDependencies(a).Length);
    }

    [TestMethod]
    public void updateTitle_MustMaintainDependencies()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(c, [a, b], TypeDependency.FS);

        _taskGraphService.UpdateTitle("c", new ProjectTask("d", "", 0));

        Assert.AreEqual(2, _taskGraphService.GetTaskDependencies(c).Length);
    }

    [TestMethod]
    public void CalculateEstimatedTime_NoDependenciesAssertDurationHourFromTask()
    {
        var a = new ProjectTask("a", "Descripcion", 5);

        _taskGraphService.Add(a);

        _taskGraphService.CalculateEstimations();

        Assert.AreEqual((TaskGraphService.GetDateTimeNow(), 0), (a.MinStartTaskDate, a.Slack));
    }

    [TestMethod]
    public void CalculateEstimatedTime_HasDependencyAssertDurationFromDependency()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();


        var fechas = (
            b.MinStartTaskDate, a.MinStartTaskDate,
            b.Slack, a.Slack
        );

        var esperado = (
            TaskGraphService.GetDateTimeNow().AddHours(0), TaskGraphService.GetDateTimeNow().AddHours(2),
            0, 0
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void CalculateEstimatedTime_HasDependenciesAssertDurationFromDependencies()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 15);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);


        _taskGraphService.CalculateEstimations();

        var dates = (
            c.MinStartTaskDate, b.MinStartTaskDate, a.MinStartTaskDate,
            c.Slack, b.Slack, a.Slack
        );

        var expected = (
            TaskGraphService.GetDateTimeNow().AddHours(0), TaskGraphService.GetDateTimeNow().AddHours(0), TaskGraphService.GetDateTimeNow().AddDays(15 / 8).AddHours(15 % 8),
            0, 13, 0
        );

        Assert.AreEqual(expected, dates);

    }

    [TestMethod]
    public void CalculateEstimatedTime_HasDependenciesTaskFinishedAssertDurationFromDependencies()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);


        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        a.State = TaskState.Finished;

        _taskGraphService.CalculateEstimations();
        var fechas = (
            c.MinStartTaskDate, b.MinStartTaskDate, a.MinStartTaskDate,
            c.Slack, b.Slack, a.Slack
        );

        var esperado = (
            DateTime.MinValue, DateTime.MinValue, DateTime.MinValue,
            int.MaxValue, int.MaxValue, int.MaxValue
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void CalculateCriticalPath_HasThreeTasksGetTitleFromAAndC()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);


        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        Assert.AreEqual(("a", "c"), (path[1].Title, path[0].Title));

    }

    [TestMethod]
    public void CalculateEstimatedTime_TakingIntoAccountSS()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);

        _taskGraphService.CalculateEstimations();

        var fechas = (
            b.MinStartTaskDate, a.MinStartTaskDate,
            b.Slack, a.Slack
        );

        var esperado = (
            TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow(),
            3, 0
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void CalculateEstimatedTime_TakingIntoAccountSS2Dependencies()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 7);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, c, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();

        var fechas = (
            c.MinStartTaskDate, b.MinStartTaskDate, a.MinStartTaskDate,
            c.Slack, b.Slack, a.Slack
        );

        var esperado = (
          TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow().AddHours(2),
          0, 0, 0
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void CalculateCriticalPath_TakingIntoAccountSS2Dependencies()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 7);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, c, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { c }, path);
    }

    [TestMethod]
    public void CalculateCriticalPath_ZeroTask()
    {
        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath();

        Assert.AreEqual(([], 0), path);
    }

    [TestMethod]
    public void GetCriticalPath_ValidProject()
    {

        var task = new ProjectTask("asdjf", "jasdkf", 10);
        var task1 = new ProjectTask("asdajf", "jasdkf", 11);

        _taskGraphService.Add(task);

        _taskGraphService.Add(task1);

        _taskGraphService.CalculateEstimations();
        var result = _taskGraphService.GetCriticalPath();

        Assert.AreEqual((task1, 11 / 8 * 24 + 11 % 8), (result.path[0], result.duration));
    }

    [TestMethod]
    public void CalculateCriticalPath_TakingIntoAccountAllSS()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 2);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(b, c, TypeDependency.SS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { c, b }, path);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithMixedSSAndFSDependencies_ReturnsCorrectPath()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);

        _taskGraphService.AddDependencyToTask(a, c, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { c, a, b }, path);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithComplexGraphOfMixedDependencies_ReturnsExpectedPath()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);

        _taskGraphService.AddDependencyToTask(a, g, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [d, b], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { g, a, b, e, f }, path);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithLongTask_ReturnsShortestPathWithLongestDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 70);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [b, d], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath();

        CollectionAssert.AreEqual(new[] { a, b }, path.path);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithLongDuration_ReturnsExpectedPath()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);
        var h = new ProjectTask("h", "Descripcion", 10);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);
        _taskGraphService.Add(h);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(h, g, TypeDependency.FS);

        _taskGraphService.AddDependencyToTaskMany(e, [b, d], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(e, h, TypeDependency.FS);

        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { g, h, e, f }, path);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithBranchingAndFinalDependencyOnLongerBranch_ReturnsExpectedPath()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);
        var h = new ProjectTask("h", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);
        _taskGraphService.Add(h);

        _taskGraphService.AddDependencyToTask(a, g, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [d, b], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(h, b, TypeDependency.FS);

        _taskGraphService.CalculateEstimations();
        var path = _taskGraphService.GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { g, a, b, h }, path);
    }

    [TestMethod]
    public void CalculateEstimatedTime_StartedTaskHour()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);


        _taskGraphService.CalculateEstimations();

        b.SetStartTaskDate(TaskGraphService.GetDateTimeNow().AddHours(1));

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        _taskGraphService.CalculateEstimations();

        var fechas = (
            c.MinStartTaskDate, b.MinStartTaskDate, a.MinStartTaskDate,
            c.Slack, b.Slack, a.Slack
        );

        var esperado = (
            TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow().AddHours(5),
            0, 2, 0
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void CalculateEstimatedTime_StartedTaskDay()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);

        _taskGraphService.CalculateEstimations();

        b.SetStartTaskDate(TaskGraphService.GetDateTimeNow().AddDays(1));

        _taskGraphService.CalculateEstimations();

        var fechas = (
            c.MinStartTaskDate, b.MinStartTaskDate, a.MinStartTaskDate,
            c.Slack, b.Slack, a.Slack
        );

        var esperado = (
            TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow().AddDays(1).AddHours(2),
            5, 0, 0
        );

        Assert.AreEqual(esperado, fechas);
    }

    [TestMethod]
    public void VerifyIfThereIsLoopWhenAddingANewDep()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(b, c, TypeDependency.SS);

        var result = _taskGraphService.VerifyValidNewEdge(c, a);

        Assert.IsInstanceOfType<Error?>(result);
    }

    [TestMethod]
    public void VerifyIfThereIsNoLoopWhenAddingANewDep()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(c, b, TypeDependency.FS);

        var result = _taskGraphService.VerifyValidNewEdge(c, a);

        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void VerifyIfThereIsLoopWithNonTrackTask_ReturnError()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);

        Assert.IsInstanceOfType<Error?>(_taskGraphService.VerifyValidNewEdge(a, b));
    }

    [TestMethod]
    public void CalculateEstimationPath_TakingIntoAccountAllSS()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 2);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(b, c, TypeDependency.SS);

        _taskGraphService.CalculateEstimations();

        var result = (
            a.MinStartTaskDate, a.Slack,
            b.MinStartTaskDate, b.Slack,
            c.MinStartTaskDate, c.Slack
            );

        var expected = (
            TaskGraphService.GetDateTimeNow(), 2,
            TaskGraphService.GetDateTimeNow(), 0,
            TaskGraphService.GetDateTimeNow(), 5
        );

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void CalculateCriticalPath_HasThreeTasksGetTitleFromAAndC_GetDuation()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTaskMany(a, [b, c], TypeDependency.FS);


        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(10 / 8 * 24 + 10 % 8, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_TakingIntoAccountSS2Dependencies_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 2);
        var c = new ProjectTask("c", "Descripcion", 7);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, c, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(7, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_TakingIntoAccountAllSS_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 2);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(b, c, TypeDependency.SS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(7, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithMixedSSAndFSDependencies_GetDuaration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);

        _taskGraphService.AddDependencyToTask(a, c, TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(12 / 8 * 24 + 12 % 8, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithComplexGraphOfMixedDependencies_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);

        _taskGraphService.AddDependencyToTask(a, g, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [d, b], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(20 / 8 * 24 + 20 % 8, duration);
    }


    [TestMethod]
    public void CalculateCriticalPath_WithLongTask_ReturnsShortestPathWithLongestDuration_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 70);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [b, d], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(75 / 8 * 24 + 75 % 8, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithLongDuration_ReturnsExpectedPath_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);
        var h = new ProjectTask("h", "Descripcion", 10);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);
        _taskGraphService.Add(h);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(h, g, TypeDependency.FS);

        _taskGraphService.AddDependencyToTaskMany(e, [b, d], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(e, h, TypeDependency.FS);

        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(25 / 8 * 24 + 25 % 8, duration);
    }

    [TestMethod]
    public void CalculateCriticalPath_WithBranchingAndFinalDependencyOnLongerBranch_ReturnsExpectedPath_GetDuration()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);
        var e = new ProjectTask("e", "Descripcion", 5);
        var f = new ProjectTask("f", "Descripcion", 5);
        var g = new ProjectTask("g", "Descripcion", 5);
        var h = new ProjectTask("h", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);
        _taskGraphService.Add(e);
        _taskGraphService.Add(f);
        _taskGraphService.Add(g);
        _taskGraphService.Add(h);

        _taskGraphService.AddDependencyToTask(a, g, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);
        _taskGraphService.AddDependencyToTaskMany(e, [d, b], TypeDependency.SS);
        _taskGraphService.AddDependencyToTask(f, e, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(h, b, TypeDependency.FS);

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(22 / 8 * 24 + 22 % 8, duration);
    }

    [TestMethod]
    public void GetCriticalPathDuration_WhenEndTaskHasFixedDate_ReturnsZero()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(c, b, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);

        d.SetEndTaskDate(TaskGraphService.GetDateTimeNow().AddDays(1000));

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(0, duration);
    }

    [TestMethod]
    public void GetCriticalPathDuration_WhenIntermediateTaskHasFixedDate_ReturnsAdjustedDuratio()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 7);
        var c = new ProjectTask("c", "Descripcion", 5);
        var d = new ProjectTask("d", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);
        _taskGraphService.Add(c);
        _taskGraphService.Add(d);

        _taskGraphService.AddDependencyToTask(b, a, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(c, b, TypeDependency.FS);
        _taskGraphService.AddDependencyToTask(d, c, TypeDependency.FS);

        b.SetEndTaskDate(TaskGraphService.GetDateTimeNow().AddDays(20000));

        var duration = _taskGraphService.GetCriticalPath().duration;

        Assert.AreEqual(10 / 8 * 24 + (10 % 8), duration);
    }

    [TestMethod]
    public void CanStartTaskWith0Deps()
    {
        var a = new ProjectTask("a", "Descripcion", 5);

        _taskGraphService.Add(a);

        var valid = _taskGraphService.ValidTaskToStart(a);

        Assert.IsTrue(valid);
    }

    [TestMethod]
    public void CanStartTaskWith1DepsWhichIsNotFinished()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        var valid = _taskGraphService.ValidTaskToStart(a);

        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void CanStartTaskWith1DepsWhichIsStartedSs()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);

        b.SetStartTaskDate(TaskGraphService.GetDateTimeNow());

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);

        var valid = _taskGraphService.ValidTaskToStart(a);

        Assert.IsTrue(valid);
    }

    [TestMethod]
    public void CanStartTaskWith1DepsWhichIsPendingSs()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.SS);

        var valid = _taskGraphService.ValidTaskToStart(a);

        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void CanStartTaskWith1DepsWhichIsFinished()
    {
        var a = new ProjectTask("a", "Descripcion", 5);
        var b = new ProjectTask("b", "Descripcion", 5);

        b.SetEndTaskDate(TaskGraphService.GetDateTimeNow());

        _taskGraphService.Add(a);
        _taskGraphService.Add(b);

        _taskGraphService.AddDependencyToTask(a, b, TypeDependency.FS);

        var valid = _taskGraphService.ValidTaskToStart(a);

        Assert.IsTrue(valid);
    }

    [TestMethod]
    public void TaskUsesResource_NoOneUses()
    {
        UserService.GetInstance();


        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };
        
        var taskDTO = new TaskDTO()
        {
            Title = "taskJ",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO1 = new TaskDTO()
        {
            Title = "taskP",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto, taskDTO);
        projectS.AddTask(_systemManager, projectDto, taskDTO1);
        projectS.AddDependencyToTask(_systemManager, projectDto, new TaskGraphService.Edge()
        {
            targetDto = taskDTO,
            depDto = taskDTO1,
            type = TypeDependency.FS
        });

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO1,
            ProjectDto = projectDto,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        var context = _contextFactory.Generate();
        var task = context.ProjectTask.First(t => t.Title == taskDTO.Title);
        var task1 = context.ProjectTask.First(t => t.Title == taskDTO1.Title);

        var actual = (
                task.MinStartTaskDate, task1.MinStartTaskDate,
                task.Slack, task1.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddHours(1), TaskGraphService.GetDateTimeNow(),
                0, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_NoOneUses_Alt()
    {
        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto);

        var project = projectS.GetProject("Pepe").Item1!;

        var taskDTO = new TaskDTO()
        {
            Title = "task",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO1 = new TaskDTO()
        {
            Title = "task1",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto, taskDTO);
        projectS.AddTask(_systemManager, projectDto, taskDTO1);
        projectS.AddDependencyToTask(_systemManager, projectDto, new TaskGraphService.Edge()
        {
            targetDto = taskDTO,
            depDto = taskDTO1,
            type = TypeDependency.FS
        });

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO,
            ProjectDto = projectDto,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        project = ProjectConstruct.ConstructDeep(projectS.GetProject("Pepe").Item1!, _contextFactory.Generate());
        
        var task = project.TaskList.Find(task => task.Title == taskDTO.Title)!;
        var task1 = project.TaskList.Find(task => task.Title == taskDTO1.Title)!;

        var actual = (
            task.MinStartTaskDate, task1.MinStartTaskDate,
            task.Slack, task1.Slack
        );

        var expected = (
            TaskGraphService.GetDateTimeNow().AddHours(1), TaskGraphService.GetDateTimeNow(),
            0, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses()
    {
        ProjectService.SpanTime = 99999;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;

        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;

        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });
        
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO10,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        new TaskGraphService(project1, _contextFactory).CalculateEstimations();

        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var actual = (
                task10.MinStartTaskDate, task11.MinStartTaskDate, task20.MinStartTaskDate,
                task10.Slack, task11.Slack, task20.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(4), TaskGraphService.GetDateTimeNow().AddHours(0), TaskGraphService.GetDateTimeNow(),
                0, 19, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_Alt()
    {
        ProjectService.SpanTime = 99999;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO11,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        
        new TaskGraphService(project1, _contextFactory).CalculateEstimations();

        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var actual = (
                task10.MinStartTaskDate, task11.MinStartTaskDate, task20.MinStartTaskDate,
                task10.Slack, task11.Slack, task20.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(5), TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(4), TaskGraphService.GetDateTimeNow(),
                0, 0, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_CriticalPath()
    {
        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO10,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());

        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var path = new TaskGraphService(project1, _contextFactory).GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { task11, task10 }, path);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_Alt_CriticalPath()
    {
        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO11,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        
        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var path = new TaskGraphService(project1, _contextFactory).GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { task11, task10 }, path);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_SS()
    {
        ProjectService.SpanTime = 99999;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.SS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO11,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        new TaskGraphService(project1, _contextFactory).CalculateEstimations();

        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var actual = (
                task10.MinStartTaskDate, task11.MinStartTaskDate, task20.MinStartTaskDate,
                task10.Slack, task11.Slack, task20.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(4), TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(4), TaskGraphService.GetDateTimeNow(),
                0, 0, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_SS_CriticalPath()
    {
        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO11,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        
        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var path = new TaskGraphService(project1, _contextFactory).GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { task11, task10 }, path);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_SS_Alt()
    {
        ProjectService.SpanTime = 99999;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.SS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO10,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        
        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());

        new TaskGraphService(project1, _contextFactory).CalculateEstimations();
        
        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var actual = (
                task10.MinStartTaskDate, task11.MinStartTaskDate, task20.MinStartTaskDate,
                task10.Slack, task11.Slack, task20.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddDays(2).AddHours(4), TaskGraphService.GetDateTimeNow(), TaskGraphService.GetDateTimeNow(),
                0, 20, 0
        );

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_SS_Alt_CriticalPath()
    {
        ProjectService.SpanTime = 0.01;
        var projectS = ProjectService.GetInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow()},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO10,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        
        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;

        var path = new TaskGraphService(project1, _contextFactory).GetCriticalPath().path;

        CollectionAssert.AreEqual(new[] { task11, task10 }, path);
    }

    [TestMethod]
    public void TaskUsesResource_SomeOneUses_FS_Started()
    {
        ProjectService.SpanTime = 99999;
        var projectS = ProjectService.GetNewInstance(_contextFactory);
        var resourceS = ResourceService.GetNewInstance(_contextFactory);

        var projectDto1 = new ProjectDTO()
        {
            Name = "Pepe",
            Description = "",
        };


        var projectDto2 = new ProjectDTO()
        {
            Name = "Pepe1",
            Description = "",
        };

        projectS.CreateProject(_systemManager, projectDto1);
        projectS.CreateProject(_systemManager, projectDto2);

        var project1 = projectS.GetProject("Pepe").Item1!;
        var project2 = projectS.GetProject("Pepe1").Item1!;
        
        projectDto1 = projectS.GetProjectDto("Pepe").Item1!;
        projectDto2 = projectS.GetProjectDto("Pepe1").Item1!;
        
        projectS.AddManagerToProject(_systemManager, _systemManager, projectDto1);
        projectS.AddManagerToProject(_systemManager, _user, projectDto2);

        var taskDTO10 = new TaskDTO()
        {
            Title = "task10",
            Description = "",
            DurationHour = 1,
        };

        var taskDTO11 = new TaskDTO()
        {
            Title = "task11",
            Description = "",
            DurationHour = 1,
        };

        projectS.AddTask(_systemManager, projectDto1, taskDTO10);
        projectS.AddTask(_systemManager, projectDto1, taskDTO11);
        taskDTO10 = projectS.GetTasksByProject(projectDto1)[0];
        taskDTO11 = projectS.GetTasksByProject(projectDto1)[1];
        projectS.AddDependencyToTask(_systemManager, projectDto1, new TaskGraphService.Edge()
        {
            targetDto = taskDTO10,
            depDto = taskDTO11,
            type = TypeDependency.FS
        });

        var taskDTO20 = new TaskDTO()
        {
            Title = "task20",
            Description = "",
            DurationHour = 20,
        };

        projectS.AddTask(_user, projectDto2, taskDTO20);
        taskDTO20 = projectS.GetTasksByProject(projectDto2)[0];

        var resourceDto = new ResourceDTO()
        {
            Name = "1",
            Type = "1",
            Description = "",
            Quantity = 1,
        };

        resourceS.CreateResource(_systemManager, resourceDto, null, 1);
        resourceS.AddResourceToTask(_user, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO20,
            ProjectDto = projectDto2,
            ResourceDto = resourceDto,
            Quantity = 1,
        });
        
        
        projectS.ChangeTaskState(_user, new ProjectService.ChangeTaskHelper()
        {
            OriginalTaskDto = taskDTO20,
            NewInfoTaskDto = new TaskDTO(){State = TaskState.Started, StartTaskDate = TaskGraphService.GetDateTimeNow().AddDays(-1).AddHours(-2)},
            ProjectDto = projectDto2,
        });

        resourceS.AddResourceToTask(_systemManager, new ResourceService.ResourceAdder()
        {
            TaskDto = taskDTO10,
            ProjectDto = projectDto1,
            ResourceDto = resourceDto,
            Quantity = 1,
        });

        project1 = ProjectConstruct.ConstructDeep(projectS.GetProject(project1.Name).Item1!, _contextFactory.Generate());
        project2 = ProjectConstruct.ConstructDeep(projectS.GetProject(project2.Name).Item1!, _contextFactory.Generate());
        new TaskGraphService(project1, _contextFactory).CalculateEstimations(TaskGraphService.GetDateTimeNow().AddDays(-1).AddHours(-2));
        new TaskGraphService(project2, _contextFactory).CalculateEstimations(TaskGraphService.GetDateTimeNow().AddDays(-1).AddHours(-2));

        var task10 = project1.TaskList.Find(task => task.Title == taskDTO10.Title)!;
        var task11 = project1.TaskList.Find(task => task.Title == taskDTO11.Title)!;
        var task20 = project2.TaskList.Find(task => task.Title == taskDTO20.Title)!;


        new TaskGraphService(project1, _contextFactory).CalculateEstimations();

        var actual = (
                task10.MinStartTaskDate, task11.MinStartTaskDate,
                task10.Slack, task11.Slack, task20.Slack
        );

        var expected = (
                TaskGraphService.GetDateTimeNow().AddDays(1).AddHours(2), TaskGraphService.GetDateTimeNow(),
                0, 9, 0
        );

        Assert.AreEqual(expected, actual);
    }
}
