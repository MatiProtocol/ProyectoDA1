using System.Linq.Expressions;
using Auxiliaries;
using DataAccess;
using Domain;
using Services.DTOs;

namespace Controllers.DBControllers;

public class ProjectController : IProjectController<Project, ProjectDTO, int>
{
    private readonly RepositoryEf<Project, int> _repository;
    
    public ProjectController(RepositoryEf<Project, int> repository)
    {
        _repository = repository;
    }

    public Error? Add(ProjectDTO entity)
    {
        return _repository.Add(entity.FromEntity().Item1!);
    }

    public ProjectDTO? GetById(int id)
    {
        var entity = _repository.GetById(id);
        return entity == null ? null : ProjectDTO.ToEntity(entity);
    } 

    public List<ProjectDTO> GetAll() => _repository.GetAll().Select(ProjectDTO.ToEntity).ToList();

    public Error? Update(ProjectDTO entity) => _repository.Update(entity.FromEntity().Item1!);

    public bool Remove(int id) => _repository.Remove(id);

    public Project? FindBy(Expression<Func<Project, bool>> predicate) => _repository.FindBy(predicate);

    public bool Exists(Expression<Func<Project, bool>> predicate) => _repository.Exists(predicate);
}