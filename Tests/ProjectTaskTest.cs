using System.Globalization;
using Auxiliaries;
using Domain;

namespace Tests;

[TestClass]
public class ProjectTaskTest
{
    private ProjectTask _taskTest = null!;

    [TestInitialize]
    public void Initialize()
    {
        _taskTest = new ProjectTask("Titulo", "Descripcion", 5);
    }

    [TestMethod]
    public void CreateNewTaskClass_NotNull()
    {
        Assert.IsNotNull(_taskTest);
    }

    [TestMethod]
    public void CreateNewTaskClassWithEmptyTitle_ReturnsError()
    {
        Assert.ThrowsException<ArgumentException>(() => new ProjectTask("", "Descripcion", 5));
    }
    
    [TestMethod]
    public void GetSetId_ReturnId()
    {
        _taskTest.Id = 1;
        var result = _taskTest.Id;
        Assert.AreEqual(1, result);
    }
    
    [TestMethod]
    public void GetSetProjectId_ReturnId()
    {
        _taskTest.ProjectId = 1;
        var result = _taskTest.ProjectId;
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void EmptyTitleSet_ThrowException()
    {
        Assert.ThrowsException<ArgumentException>(() =>_taskTest.Title = "");
    }

    [TestMethod]
    public void TitleSetGet_AssignCorrectly()
    {
        _taskTest.Title = "Package";
        var result = _taskTest.Title;
        
        Assert.AreEqual("Package", result);
    }

    [TestMethod]
    public void DescriptionSetGet_AssignCorrectly()
    {
        _taskTest.Description = "TinyDesc";
        var result = _taskTest.Description;
        
        Assert.AreEqual("TinyDesc", result);
    }

    [TestMethod]
    public void DurationHourSetGet_AssignCorrectly()
    {
        _taskTest.DurationHours = 25;
        var result = _taskTest.DurationHours;
        
        Assert.AreEqual(25, result);
    }

    [TestMethod]
    public void SetGetMinEndTaskDate_AssignCorrectly()
    {
        var expectedDate = DateTime.Today.AddDays(5);
        
        _taskTest.MinStartTaskDate = expectedDate;
        var result = _taskTest.MinStartTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }

    [TestMethod]
    public void SetGetMaxEndTaskDate_AssignCorrectly()
    {
        var expectedDate = 5;
        
        _taskTest.Slack = expectedDate;
        var result = _taskTest.Slack;
        
        Assert.AreEqual(expectedDate, result);
    }

    [TestMethod]
    public void StateSetGet_AssignCorrectly()
    {
        _taskTest.State = TaskState.Pending;
        var result = _taskTest.State;
        
        Assert.AreEqual(TaskState.Pending, result);
    }

    [TestMethod]
    public void GetSetEndTaskDate_AssignCorrectly()
    {
        var expectedDate = DateTime.Parse("10/07/2004", CultureInfo.CreateSpecificCulture("es-UY"));
        
        _taskTest.SetEndTaskDate(expectedDate);
        var result = _taskTest.EndTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void GetSetEndTaskDatePureSet_AssignCorrectly()
    {
        var expectedDate = DateTime.Parse("10/07/2004", CultureInfo.CreateSpecificCulture("es-UY"));
        
        _taskTest.EndTaskDate = expectedDate;
        var result = _taskTest.EndTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetStartTaskDate_AssignCorrectly()
    {
        var expectedDate = DateTime.Parse("10/07/2004", CultureInfo.CreateSpecificCulture("es-UY"));
        
        _taskTest.SetStartTaskDate(expectedDate);
        var result = _taskTest.StartTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetStartTaskDatePureSet_AssignCorrectly()
    {
        var expectedDate = DateTime.Parse("10/07/2004", CultureInfo.CreateSpecificCulture("es-UY"));
        
        _taskTest.StartTaskDate = expectedDate;
        var result = _taskTest.StartTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetStartTaskDate_HasToSetStateToStarted()
    {
        var expectedDate = DateTime.Parse("10/07/2004", CultureInfo.CreateSpecificCulture("es-UY"));
        
        _taskTest.SetStartTaskDate(expectedDate);
        var result = _taskTest.State;
        
        Assert.AreEqual(TaskState.Started, result);
    }
    
    [TestMethod]
    public void SetGetProgrammedDateToStart_returnDate()
    {
        var expectedDate = DateTime.Parse("10/07/2026", CultureInfo.CreateSpecificCulture("es-UY"));

        _taskTest.SetPlanedTaskDate(expectedDate);
        var result = _taskTest.StartTaskDate;
        
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetGetProgrammedDateToStart_State()
    {
        var expectedDate = DateTime.Parse("10/07/2026", CultureInfo.CreateSpecificCulture("es-UY"));

        _taskTest.SetPlanedTaskDate(expectedDate);
        var result = _taskTest.State;
        Assert.AreEqual(TaskState.Planed, result);
    }
    
    [TestMethod]
    public void SetGetProgrammedDateToStart_GetDate()
    {
        _taskTest.MinStartTaskDate = DateTime.MinValue;
        var expectedDate = DateTime.Parse("10/07/2026", CultureInfo.CreateSpecificCulture("es-UY"));

        _taskTest.SetPlanedTaskDate(expectedDate);
        var result = _taskTest.GetStartDate();
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetGeteStartDate_GetDate()
    {
        _taskTest.MinStartTaskDate = DateTime.MinValue;
        var expectedDate = DateTime.Parse("10/07/2026", CultureInfo.CreateSpecificCulture("es-UY"));

        _taskTest.SetStartTaskDate(expectedDate);
        var result = _taskTest.GetStartDate();
        Assert.AreEqual(expectedDate, result);
    }
    
    [TestMethod]
    public void SetGeteMinStart_GetDate()
    {
        _taskTest.MinStartTaskDate = DateTime.MinValue;

        var result = _taskTest.GetStartDate();
        Assert.AreEqual(DateTime.MinValue, result);
    }
    
    [TestMethod]
    public void SetGeteEndDate_GetEndDate()
    {
        var result = _taskTest.GetEndDate();
        Assert.AreEqual(_taskTest.GetStartDate().AddDays(_taskTest.DurationHours / 8).AddHours(_taskTest.DurationHours % 8), result);
    }
}
