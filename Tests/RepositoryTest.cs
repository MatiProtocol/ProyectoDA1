using DataAccess;
using Auxiliaries;

namespace Tests;

[TestClass]
public class RepositoryTest
{

    [TestMethod]
    public void CreateNewRepository_NotNull()
    {
        var result = new Repository<int, int>();

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void AddElement_HaveGetIt()
    {
        var r = new Repository<int, int>();

        r.Add(1, 2);
        var result = r.Get(1).Value;

        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void AddElementWithExistingKey_ReturnError()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        var result = r.Add(1, 3);

        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void AddTriggerReactive()
    {
        var r = new Repository<int, int>();

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        r.Add(1, 1);


        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void GetElementWhichDoesNotExist_HasMustBeFalse()
    {
        var r = new Repository<int, int>();

        var result = r.Get(1).Has;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveValue_AddedValueShouldNotBe()
    {
        var r = new Repository<int, int>();
        r.Add(2, 2);

        r.Remove(2);
        var result = r.Get(1).Has;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveValueThatDoesNotExist_AddedValueShouldNotBe()
    {
        var r = new Repository<int, int>();

        var result = r.Remove(2);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void RemoveValueThatExist_InvokesReactive()
    {
        var r = new Repository<int, int>();
        r.Add(1, 1);

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        r.Remove(1);

        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void RemoveValueThatItDoesNotExist_NotInvokesReactive()
    {
        var r = new Repository<int, int>();

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        _ = r.Remove(2);

        Assert.AreEqual(0, counter);
    }

    [TestMethod]
    public void UpdateValue_MustGetNewValue()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        r.UpdateValue(1, 3);
        var result = r.Get(1).Value;

        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void UpdateValue_WhenKeyDoesExist_TriggerReactive()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        r.UpdateValue(1, 3);

        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void UpdateValue_WhenKeyDoesNotExist_NotTriggerReactive()
    {
        var r = new Repository<int, int>();

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;

        r.UpdateValue(1, 3);

        Assert.AreEqual(0, counter);
    }

    [TestMethod]
    public void UpdateKey_MustPerseveValue()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        r.UpdateKey(1, 3);
        var result = r.Get(3).Value;

        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void UpdateKeyWithNonExistingKey_ReturnError()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        var result = r.UpdateKey(2, 4);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UpdateKeyWithNonExistingKey_NotTriggerReactive()
    {
        var r = new Repository<int, int>();

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;


        r.UpdateKey(2, 4);


        Assert.AreEqual(0, counter);
    }

    [TestMethod]
    public void UpdateKeyWithExistingKey_TriggerReactive()
    {
        var r = new Repository<int, int>();
        r.Add(2, 2);

        var react = Reactivity.GetInstance();
        var counter = 0;
        react.OnChange += () => counter++;


        r.UpdateKey(2, 4);

        Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void UpdateKeyWithExistingNewKey_ReturnError()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);
        r.Add(2, 3);

        var result = r.UpdateKey(1, 2);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UpdateValueWhithNonExistingKey_ReturnError()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);

        var result = r.UpdateValue(2, 3);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void GetAllItems()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);
        r.Add(3, 5);

        //Act
        var all = r.GetAll();
        var expected = new[] { (1, 2), (3, 5) };

        //Assert
        CollectionAssert.AreEqual(expected, all);
    }

    [TestMethod]
    public void GetValues_ReturnAllValues()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);
        r.Add(2, 3);

        var values = r.GetValues();

        Assert.IsTrue(values is [2, 3]);
    }

    [TestMethod]
    public void RepositoryReset_LosesAllKV()
    {
        var r = new Repository<int, int>();
        r.Add(1, 2);
        r.Add(2, 3);

        r.Reset();
        
        var values = r.GetValues();
        
        Assert.AreEqual(0, values.Length);
    }
}
