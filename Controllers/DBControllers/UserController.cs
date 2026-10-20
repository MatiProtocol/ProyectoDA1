
using System.Linq.Expressions;
using Auxiliaries;
using DataAccess;
using Domain;
using Services.DTOs;

namespace Controllers.DBControllers;

public class UserController : IUserController<User, UserDTO, int>
{
    private readonly RepositoryEf<User, int> _repository;
    
    public UserController(RepositoryEf<User, int> repository)
    {
        _repository = repository;
    }

    public Error? Add(UserDTO entity) => _repository.Add(entity.FromEntity([1, 2, 3]).Item1!);

    public UserDTO? GetById(int id)
    {
        var entity = _repository.GetById(id);
        return entity == null ? null : UserDTO.ToEntity(entity);
    } 

    public List<UserDTO> GetAll() => _repository.GetAll().Select(UserDTO.ToEntity).ToList();

    public Error? Update(UserDTO entity) => _repository.Update(entity.FromEntity([1,2,3]).Item1!);

    public bool Remove(int id) => _repository.Remove(id);

    public User? FindBy(Expression<Func<User, bool>> predicate) => _repository.FindBy(predicate);

    public bool Exists(Expression<Func<User, bool>> predicate) => _repository.Exists(predicate);
}
