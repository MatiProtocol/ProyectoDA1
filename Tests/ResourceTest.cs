using Domain;

namespace Tests;

[TestClass]
public class ResourceTest
{
    private Resource _resource = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _resource = new Resource("asdasd", "erer", "aszxc", 1);
    }

    [TestMethod]
    public void CreateResource_IsNotNull()
    {
        Assert.IsNotNull(_resource);
    }

    [TestMethod]
    public void SetGetName_AssignCorrectly()
    {
        _resource.Name = "Programmer";
        
        var result = _resource.Name;

        Assert.AreEqual("Programmer", result);
    }

    [TestMethod]
    public void SetGetType_AssignCorrectly()
    {
        _resource.Type = "piripichi";
        
        var result = _resource.Type;

        Assert.AreEqual("piripichi", result);
    }

    [TestMethod]
    public void SetGetDescription_AssignCorrectly()
    {
        var expectedDescription = "parangutirimiculo";

        _resource.Description = expectedDescription;
        
        var result = _resource.Description;

        Assert.AreEqual(expectedDescription, result);
    }

    [TestMethod]
    public void SetGetExclusiveAfterCreateAResource_ReturnFalse()
    {
        var result = _resource.Exclusive;

        _resource.Exclusive = false;

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ToggleExclusiveness_ToggleCorrectly()
    {
        var beforeToggle = _resource.Exclusive;

        _resource.ToggleExclusiveness();
        var afterToggle = _resource.Exclusive;

        Assert.IsFalse(beforeToggle);
        Assert.IsTrue(afterToggle);
    }
    
    [TestMethod]
    public void CreateNewResourceEmpty_ThrowError()
    {
        string? error = null;
        
        try
        {
            _ = new Resource("", "", "", 0);
        }
        catch (ArgumentException e)
        {
            error = e.Message;
        }
        
        Assert.AreEqual(error, "El nombre del recurso no puede ser vacio.");
    }
    
    [TestMethod]
    public void SetResourceNameEmpty_ThrowError()
    {
        try
        {
            _resource.Name = "";
        }
        catch (ArgumentException e)
        {
            Assert.AreEqual(e.Message, "El nombre del recurso no puede ser vacio.");
        }
    }
    
    [TestMethod]
    public void SetGetID_AssignCorrectly()
    {
        var expectedId = 1;
        
        _resource.Id = expectedId;
        
        Assert.AreEqual(_resource.Id, expectedId);
    }

    [TestMethod]
    public void SetGetQuantity_AssignCorrectly()
    {
        _resource.Quantity = 1;
        Assert.AreEqual(_resource.Quantity, 1);
    }
    
    [TestMethod]
    public void  GetQueue()
    {
        Assert.IsInstanceOfType<List<(ProjectTask, int, int)>>(_resource.Queue);
    }
}