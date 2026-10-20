using Auxiliaries;
using Domain;

namespace Tests;

[TestClass]
public class TaskGraphTest
{
    [TestMethod]
    public void CreateNewTaskGraphClass_NotNull()
    {
        Assert.IsNotNull(new TaskGraph());
    }

    [TestMethod]
    public void SetGetEndOfGraph_()
    {
        var taskGraph = new TaskGraph
        {
            EndOfGraph = new ProjectTask("example", "", 0)
        };
        var task = taskGraph.EndOfGraph;
        _ = taskGraph.EndOfGraph;
        Assert.AreEqual("example", task.Title);
    }

    [TestMethod]
    public void SetGetDeptGraph_IsNotNull()
    {
        var taskGraph = new TaskGraph
        {
            DepGraph = new Dictionary<string, List<(TypeDependency type, ProjectTask task)>>()
        };

        Assert.IsNotNull(taskGraph.DepGraph);
    }

    [TestMethod]
    public void SetGetGraph_IsNotNull()
    {
        var taskGraph = new TaskGraph
        {
            Graph = new Dictionary<string, List<(TypeDependency type, ProjectTask task)>>()
        };

        Assert.IsNotNull(taskGraph.Graph);
    }

    [TestMethod]
    public void GetSetTimeTable_NotNull()
    {
        TaskGraph taskGraph = new TaskGraph();

        taskGraph.TimeTable = new Dictionary<string, int>();
        Assert.IsNotNull(taskGraph.TimeTable);
    }

    [TestMethod]
    public void GetSetNameToTask_NotNull()
    {
        TaskGraph taskGraph = new TaskGraph();

        taskGraph.NameToTask = new Dictionary<string, ProjectTask>();
        Assert.IsNotNull(taskGraph.NameToTask);
    }
}
