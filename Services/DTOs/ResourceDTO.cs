using Auxiliaries;
using Domain;

namespace Services.DTOs;

public class ResourceDTO
{

    public int Id;
    public string Name;
    public string Type;
    public string Description;
    public bool IsExclusive;
    public int Quantity;
    public List<(TaskDTO task, int quantity, int priority)> Queue;

    public (Resource?, Error?) FromEntity()
    {
        try
        {
            var resource = new Resource(Name, Type, Description, Quantity);
            resource.Id = Id;
            return (resource, null);
        }
        catch (Exception e)
        {
            return (null, new Error("ArgumentError", e.Message));
        }
    }

    public static ResourceDTO ToEntity(Resource resource)
    {
        var resourceDto = new ResourceDTO
        {
            Name = resource.Name,
            Description = resource.Description,
            Type = resource.Type,
            IsExclusive = resource.Exclusive,
            Quantity = resource.Quantity,
            Queue = resource.Queue.Select(t => (TaskDTO.ToEntity(t.task), t.quantity, t.priority)).ToList(),
            Id = resource.Id
        };
        return resourceDto;
    }

}
