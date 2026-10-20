using System.Linq.Expressions;
using Auxiliaries;
using DataAccess;
using Domain;
using Services.DTOs;

namespace Controllers.DBControllers;

public class ResourceController : IUserController<Resource, ResourceDTO, int>
{
    private readonly RepositoryEf<Resource, int> _repository;
    
    public ResourceController(RepositoryEf<Resource, int> repository)
    {
        _repository = repository;
    }
    public Error? Add(ResourceDTO entity)
    {
        return _repository.Add(entity.FromEntity().Item1!);
    }

    public ResourceDTO? GetById(int id)
    {
        var entity = _repository.GetById(id);
        return entity == null ? null : ResourceDTO.ToEntity(entity);
    }

    public List<ResourceDTO> GetAll()
    {
        return _repository.GetAll().Select(ResourceDTO.ToEntity).ToList();
    }

    public Error? Update(ResourceDTO entity)
    {
        return _repository.Update(entity.FromEntity().Item1!);
    }

    public bool Remove(int id)
    {
        return _repository.Remove(id);
    }

    public Resource? FindBy(Expression<Func<Resource, bool>> predicate)
    {
        return _repository.FindBy(predicate);
    }

    public bool Exists(Expression<Func<Resource, bool>> predicate)
    {
        return _repository.Exists(predicate);
    }
}