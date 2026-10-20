using System.Linq.Expressions;
using Auxiliaries;
using DataAccess;
using Domain;
using Services.DTOs;

namespace Controllers.DBControllers;

public class TaskController : IUserController<ProjectTask, TaskDTO, int>
{
    private readonly RepositoryEf<ProjectTask, int> _repository;
    
    public TaskController(RepositoryEf<ProjectTask, int> repository)
    {
        _repository = repository;
    }
    
    public Error? Add(TaskDTO entity)
    {
        return _repository.Add(entity.FromEntity().Item1!);
    }

    public TaskDTO? GetById(int id)
    {
        var entity = _repository.GetById(id);
        return entity == null ? null : TaskDTO.ToEntity(entity);
    } 

    public List<TaskDTO> GetAll() => _repository.GetAll().Select(TaskDTO.ToEntity).ToList();

    public Error? Update(TaskDTO entity) => _repository.Update(entity.FromEntity().Item1!);

    public bool Remove(int id) => _repository.Remove(id);

    public ProjectTask? FindBy(Expression<Func<ProjectTask, bool>> predicate) => _repository.FindBy(predicate);

    public bool Exists(Expression<Func<ProjectTask, bool>> predicate) => _repository.Exists(predicate);
}