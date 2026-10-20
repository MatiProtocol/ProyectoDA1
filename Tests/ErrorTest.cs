using Auxiliaries;

namespace Tests;

[TestClass]
public class ErrorTest
{
    private Error _error = null!;
    [TestInitialize]
    public void Initialize()
    {
        _error = new Error("DefaultError","AhhAhhh");
    }

    [TestMethod]
    public void CreateNewError_ShouldIsNotNull()
    {
        Assert.IsNotNull(_error);
    }

    [TestMethod]
    public void SetGetFormatErrorType_ShouldAssignAndReturnCorrectly()
    {
        _error.SetType("FormatError");
        
        var result = _error.GetType();
        
        Assert.AreEqual("FormatError", result);
    }

    [TestMethod]
    public void SetGetOutOfRangeExceptionType_ShouldAssignAndReturnCorrectly()
    {
        _error.SetType("OUtOfRAngeExcepTion");
        
        var result = _error.GetType();
        
        Assert.AreEqual("OutOfRangeException", result);
    }

    [TestMethod]
    public void SetGetMessage_ShouldAssignAndReturnCorrectly()
    {
        _error.SetMessage("errorsino");
        
        var result = _error.GetMessage();
        
        Assert.AreEqual("errorsino", result);
    }

    [TestMethod]
    public void SetGetPermissionDenied_ShouldAssignAndReturnCorrectly()
    {
        _error.SetType("PermissionDenied");
        
        var result = _error.GetType();
        
        Assert.AreEqual("PermissionDenied", result);
    }

    [TestMethod]
    public void CreatingErrorWithTypeAndMessage_MustBeSet()
    {
        _error.SetType("formaterror");
        
        var result = _error.GetType();
        
        
        Assert.AreEqual("FormatError", result);
    }

    [TestMethod]
    public void SetTypeWithInvalidType_throwException()
    {
        
        Assert.ThrowsException<ArgumentException>(() => _error.SetType("InvalidType45234"));
    }

    [TestMethod]
    public void CreatErrorWithEmptyConstructor_IsNotNull()
    {
        var error = new Error();
        
        
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void SetArgumentError_MustBeSet()
    {
        _error.SetType("Argumenterror");
        
        var result = _error.GetType();
        
        
        Assert.AreEqual("ArgumentError", result);
    }
    
    [TestMethod]
    public void SetArgumentErrorButTheEnum_MustBeSet()
    {
        _error.SetType(Error.Types.ArgumentError);
        
        var result = _error.GetType();
        
        
        Assert.AreEqual("ArgumentError", result);
    }
    
    [TestMethod]
    public void SetArgumentErrorButWithEnum_MustBeSet()
    {
        var error = new Error(Error.Types.ArgumentError, "Pepe");
        
        var result = error.GetType();
        
        
        Assert.AreEqual("ArgumentError", result);
    }
    
    [TestMethod]
    public void SetMessageButWithEnum_MustBeSet()
    {
        var error = new Error(Error.Types.ArgumentError, "Pepe");
        
        var result = error.GetMessage();
        
        
        Assert.AreEqual("Pepe", result);
    }
}
