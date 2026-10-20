using Auxiliaries;
using Domain;
using Services.DTOs;

namespace Tests;

[TestClass]
public class ResourceDtoTest
{
    [TestMethod]
    public void ToEntity_ConvertCorrectly()
    {
        var resource = new Resource("piripichi", "parangutirimiculo", "patidifuso", 1);
        
        var result = ResourceDTO.ToEntity(resource);
        
        Assert.IsInstanceOfType<ResourceDTO>(result);
    }

    [TestMethod]
    public void FromEntity_ConvertCorrectly()
    {
        var resourceDto = new ResourceDTO
        {
            Name = "piripichi",
            Description = "parangutirimiculo",
            Type = "awnson"
        };
        
        var resource = resourceDto.FromEntity().Item1;
        
        Assert.IsInstanceOfType<Resource>(resource);
    }
    
    [TestMethod]
    public void FromEntity_ConvertIncorrectly()
    {
        var resource = new ResourceDTO
        {
            Name = "",
            Description = "parangutirimiculo",
            Type = "trol"
        };
        
        var error = resource.FromEntity().Item2;
        
        Assert.IsInstanceOfType<Error>(error);
    }
}