using System.Linq.Expressions;
using Auxiliaries;
using Controllers.DBControllers;
using DataAccess;
using DBContext;
using Domain;
using Microsoft.IdentityModel.Tokens;
using Services.DTOs;

namespace TestDB;


[TestClass]
public class ResourceControllerTest
{
    private ISqlContextFactory _contextFactory;
    private ResourceController _resourceController;
    private RepositoryEf<Resource, int> _repository;
    private int _i = 0;

    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("ResourceControllerTest" + _i);
        _repository = new RepositoryEf<Resource, int>(_contextFactory, UniqueNameTypeRule, resource => resource.Id);
        _resourceController = new ResourceController(_repository);
        return;
        Expression<Func<Resource, bool>> UniqueNameTypeRule(Resource resource) => r => r.Name == resource.Name && r.Type == resource.Type && r.Id != resource.Id;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }
    
    [TestMethod]
    public void AddResource_AddResourceCorrectly()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        var error = _resourceController.Add(resopurceDto);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void AddNewResourceWithTrackedNameAndType_ReturnError()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        resopurceDto.Id = 0;
        var error = _resourceController.Add(resopurceDto);
        
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void FindById_ReturnResource()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var result = _resourceController.FindBy(r => r.Name == resopurceDto.Name);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void FindByIdNonTrackedResource_ReturnError()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        
        var result = _resourceController.FindBy(r => r.Name == resopurceDto.Name);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void DeleteResourceTracked_ReturnTrue()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var resource = _resourceController.FindBy(r => r.Name == resopurceDto.Name);
        if (resource == null) return;
        
        var deleted = _resourceController.Remove(resource.Id);
        Assert.IsTrue(deleted);
    }
    
    [TestMethod]
    public void DeleteResourceNotTrackedId_ReturnFalse()
    {
        var deleted = _resourceController.Remove(-1);
        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public void GetById_ReturnResource()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var result = _resourceController.GetById(1);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetByIdWithNotTrackedIf_ReturnNull()
    {
        var result = _resourceController.GetById(1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetAllResourcesWithNoAdded_ReturnNull()
    {
        var result = _resourceController.GetAll();
        Assert.IsTrue(result.IsNullOrEmpty());
    }

    [TestMethod]
    public void GetAllResourcesWithResources_ReturnResources()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var result = _resourceController.GetAll();
        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public void UpdateWithNonTrackUser_ReturnError()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        
        var result = _resourceController.Update(resopurceDto);
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UpdateResource_UpdateResourceCorrectly()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var resource = _resourceController.FindBy(r => r.Name == resopurceDto.Name);
        
        resource.Name = "Andresito";
        _resourceController.Update(ResourceDTO.ToEntity(resource));
        
        var newResourceInfo = _resourceController.FindBy(r => r.Name == resource.Name);
        
        Assert.AreEqual(newResourceInfo.Name, resource.Name);
    }

    [TestMethod]
    public void ExistsWithTrackedResource_ReturnTrue()
    {
        var resopurceDto = new ResourceDTO
        {
            Name = "Titulo",
            Type = "Descripcion",
            Description = "Descripcion",
            Id = 1,
            IsExclusive = false,
            Quantity = 1
        };
        _resourceController.Add(resopurceDto);
        
        var result = _resourceController.Exists(r => r.Name == resopurceDto.Name);
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void ExistsWithNonTrackedResource_ReturnTrue()
    {
        var result = _resourceController.Exists(r => r.Name == "resopurceDto.Name");
        Assert.IsFalse(result);
    }
}