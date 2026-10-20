using Auxiliaries;
using Domain;
using Services.DTOs;

namespace Tests;

[TestClass]
public class UserDtoTest
{
    [TestMethod]
    public void ToEntity_ConvertCorrectly()
    {
        var user = new User("tata", "tata", "tata@tatita.com", DateTime.Parse("10/04/2004"), [1, 2, 3]);
        
        var result = UserDTO.ToEntity(user);
        
        Assert.IsInstanceOfType<UserDTO>(result);
    }

    [TestMethod]
    public void FromEntity_ConvertCorrectly()
    {
        var userDto = new UserDTO()
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        var user = userDto.FromEntity([1, 2, 3]).Item1;
        
        Assert.IsInstanceOfType<User>(user);
    }

    [TestMethod]
    public void FromEntityWithInvalidUser_returnError()
    {
        var userDto = new UserDTO()
        {
            Name = "tata",
            LastName = "tata",
            Email = "tatatatita",
            BirthDate = "10/04/2004"
        };
        
        var error = userDto.FromEntity([1, 2, 3]).Item2;
        
        Assert.IsInstanceOfType<Error>(error);
    }
}
