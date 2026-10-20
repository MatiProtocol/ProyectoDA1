using Auxiliaries;

namespace Tests;

[TestClass]
public class ReactivityTest
{
    [TestMethod]
    public void New_NotNull()
    {
        var reactivity = Reactivity.GetInstance();

        Assert.IsNotNull(reactivity);
    }

    [TestMethod]
    public void AddAnAction_AndExecute()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        react.Invoke();

        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void AddAnAction_AndExecuteTwice()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        react.Invoke();
        react.Invoke();

        Assert.AreEqual(2, counter);
    }

    [TestMethod]
    public void AddTwoActions_AndExecute()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;
        react.OnChange += () => counter++;

        react.Invoke();

        Assert.AreEqual(2, counter);
    }

    [TestMethod]
    public void AddAnActionTaskGraph_AndExecute()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChangeTaskGraph += () => counter++;

        react.InvokeTaskGraph();

        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void AddAnActionTaskGraph_AndExecuteTwice()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChangeTaskGraph += () => counter++;

        react.InvokeTaskGraph();
        react.InvokeTaskGraph();

        Assert.AreEqual(2, counter);
    }

    [TestMethod]
    public void AddTwoActionsTaskGraph_AndExecute()
    {
        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChangeTaskGraph += () => counter++;
        react.OnChangeTaskGraph += () => counter++;

        react.InvokeTaskGraph();

        Assert.AreEqual(2, counter);
    }
}
