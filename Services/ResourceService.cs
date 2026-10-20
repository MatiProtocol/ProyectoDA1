using System.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Domain.HelpersDB;
using DomainConstruct;
using Services.DTOs;

namespace Services;

public class ResourceService
{
    private PureDatabase _database;
    private ISqlContextFactory _contextFactory;
    private static ResourceService? _instance;
    private static bool _isTest;

    public static ResourceService GetInstance(ISqlContextFactory? contextFactory = null)
    {
        if (_instance == null) _instance = new ResourceService();
        _instance._database = PureDatabase.GetNewInstance(contextFactory);
        return _instance;
    }
    private ResourceService(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        _contextFactory = contextFactory;
    }

    public static ResourceService GetNewInstance(ISqlContextFactory? contextFactory = null)
    {
        var instance = new ResourceService(contextFactory);
        instance._database = PureDatabase.GetNewInstance(contextFactory);
        return instance;
    }

    public struct ResourceAdder
    {
        public ResourceDTO ResourceDto;
        public int Quantity;
        public ProjectDTO ProjectDto;
        public TaskDTO TaskDto;
    }

    private (User? user, Error? error) HasPermissionToEdit(UserDTO aplicant)
    {
        var user = _database.Users.FindBy(u => u.Email.Equals(aplicant.Email));
        if (user == null) return (null, new Error(Error.Types.KeyDoesNotExist, "Aplicante no existe"));

        if (user.Role == Roles.ProjectMember) return (null, new Error(Error.Types.PermissionDenied, "Permisos insuficientes para operación"));

        return (user, null);
    }

    public Error? CreateResource(UserDTO solicitant, ResourceDTO newResourceDto, ProjectDTO? projectExclusive,
        int quantity)
    {
        var (_, userError) = HasPermissionToEdit(solicitant);

        if (userError != null) return userError;

        var (resource, error) = newResourceDto.FromEntity();
        if (resource == null) return error;

        var exist = _database.Resources.Exists(r => r.Type == resource.Type && r.Name == resource.Name);
        if (exist) return new Error("KeyAlreadyExist", "El recurso ya existe");

        resource.Quantity = quantity;
        return _database.Resources.Add(resource) != null
            ? new Error(Error.Types.KeyAlreadyExist, "El recurso ya existe")
            : projectExclusive != null ? CreateResourceExclusive(projectExclusive, resource) : null;
    }

    private Error? CreateResourceExclusive(ProjectDTO projectDto, Resource resource)
    {
        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var (project, error) = projectService.GetProject(projectDto.Name);
        if (project == null) return error;

        var projectResourceExclusive = new ProjectResourceExclusive
        {
            ResourceId = resource.Id,
            ProjectId = project.Id,
            Quantity = resource.Quantity
        };
        return _database.ProjectResourceExclusives.Add(projectResourceExclusive);
    }

    public Error? DeleteResource(UserDTO solicitant, int id)
    {
        var (_, userError) = HasPermissionToEdit(solicitant);

        if (userError != null) return userError;

        var resource = _database.Resources.FindBy(r => r.Id == id);
        if (resource == null) return new Error("KeyDoesNotExist", "El recurso no existe");

        // ERROR: Check if there is a relation with many quantity
        if (resource.Quantity > GetResourceTotalUses(ResourceDTO.ToEntity(resource)))
        {
            resource.Quantity--;
        }
        else return new Error("DefaultError", "No se puede borrar más de lo utilizado");

        if (resource.Quantity >= 1) return _database.Resources.Update(resource);

        return !_database.Resources.Remove(resource.Id)
            ? new Error(Error.Types.DatabaseUpdateError, "Error al borrar el recurso")
            : null;
    }

    public (List<ResourceDTO>?, Error?) GetResourceDtoLst(string resourceType)
    {
        if (!_database.Resources.Exists(r => r.Type == resourceType))
            return ([], new Error("KeyDoesNotExist", "El recurso no existe"));

        return (_database.Resources.GetAll().Where(r => r.Type == resourceType).Select(ResourceDTO.ToEntity).ToList(), null);
    }

    public Error? ChangeNameResource(UserDTO solicitant, ProjectDTO projectDto, ResourceDTO resourceDto, string newName)
    {
        var (user, userError) = HasPermissionToEdit(solicitant);
        if (user == null) return userError;

        if (string.IsNullOrEmpty(newName)) return new Error("ArgumentError", "El nombre no puede estar vacio");

        var resource = _database.Resources.FindBy(r => r.Name == resourceDto.Name && r.Type == resourceDto.Type);
        if (resource == null) return new Error("KeyDoesNotExist", "El recurso no existe");

        if (user.Role == Roles.ProjectManager && !resourceDto.IsExclusive)
        {
            if (ResourceIsUsed(resourceDto, projectDto))
                return new Error("ArgumentError", "El recurso se encuentra en más de un proyecto");
        }

        resource.Name = newName;
        _database.Resources.Update(resource);

        return null;
    }

    public bool ResourceIsUsed(ResourceDTO resourceDto, ProjectDTO projectDto)
    {
        var projectService = ProjectService.GetNewInstance(_contextFactory);
        var (project, _) = projectService.GetProject(projectDto.Name);
        if (project == null) return false;
        return _database.Resources.Exists(u => u.Name == resourceDto.Name && u.Type == resourceDto.Type)
               &&
               _database.TaskResources.Query(context =>
                   context.TaskResource.Join(
                       context.ProjectTask.Where(t => t.ProjectId == project.Id),
                       tr => tr.ProjectTaskId,
                       t => t.Id,
                       (task, _) => task
                   )
               ).Any();
    }

    public Error? ChangeDescriptionResource(UserDTO solicitant, ProjectDTO projectDto, ResourceDTO resourceDto,
        string newDescription)
    {
        var (user, userError) = HasPermissionToEdit(solicitant);

        if (user == null) return userError;

        if (string.IsNullOrEmpty(newDescription))
            return new Error("ArgumentError", "La descripcion no puede estar vacia");

        var resource = _database.Resources.FindBy(r => r.Name == resourceDto.Name && r.Type == resourceDto.Type);
        if (resource == null) return new Error("KeyDoesNotExist", "El recurso no existe");

        if (user.Role == Roles.ProjectManager && !resourceDto.IsExclusive)
        {
            if (ResourceIsUsed(resourceDto, projectDto))
                return new Error("ArgumentError", "El recurso se encuentra en más de un proyecto");
        }

        resource.Description = newDescription;

        _database.Resources.Update(resource);
        return null;
    }

    public Error? AddResourceToTask(UserDTO applicant, ResourceAdder resourceData)
    {
        var (_, userError) = HasPermissionToEdit(applicant);

        if (userError != null) return userError;

        var existence = _database.Resources.Exists(r => r.Type == resourceData.ResourceDto.Type);
        if (!existence) return new Error("KeyDoesNotExist", "El tipo de recurso no existe");

        var resource = _database.Resources.FindBy(r =>
            r.Name == resourceData.ResourceDto.Name && r.Type == resourceData.ResourceDto.Type);
        if (resource == null) return new Error("KeyDoesNotExist", "El recurso no existe");

        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var (project, error) = projectService.GetProject(resourceData.ProjectDto.Name);
        if (project == null) return error;
        project = ProjectConstruct.ConstructDeep(project, _database._contextFactory.Generate());

        if (resource.Exclusive)
        {
            var usageError = ResourceExclusiveAvailable(resourceData.ProjectDto, resourceData.ResourceDto);
            if (usageError != null) return usageError;
        }

        if (resourceData.Quantity > resource.Quantity) return new Error("ArgumentError", "La cantidad de recurso a asignar es mayor que la total");

        if (!project.TaskList.Exists(task => task.Title == resourceData.TaskDto.Title)) return new Error("KeyDoesNotExist", "La tarea no existe");

        var task = project.TaskList.Find(task => task.Title == resourceData.TaskDto.Title);
        if (task == null) return new Error("KeyDoesNotExist", "La tarea no existe");
        
        if (task.State != TaskState.Pending) return new Error(Error.Types.PermissionDenied, "La tarea ya fue completada");

        project.AddResourceRelation(task.Title, resource, resourceData.Quantity);

        if (resourceData.ResourceDto.IsExclusive) return AddProjectResourceExclusive(project, resource);
        return AddTaskResource(task, resource, resourceData.Quantity);
    }

    private Error? AddProjectResourceExclusive(Project project, Resource resource)
    {
        var projectTaskExclusive = new ProjectResourceExclusive
        {
            ResourceId = resource.Id,
            ProjectId = project.Id
        };

        Reactivity.GetInstance().InvokeTaskGraph();
        return _database.ProjectResourceExclusives.Add(projectTaskExclusive);
    }

    private Error? AddTaskResource(ProjectTask task, Resource resource, int quantity)
    {
        var allRes = _contextFactory.Generate().TaskResource.Where(tr => tr.ResourceId == resource.Id).Select(tr => tr.Priority);
        var taskResource = new TaskResource
        {
            ResourceId = resource.Id,
            ProjectTaskId = task.Id,
            Priority = !allRes.Any() ? 0 : allRes.Max() + 1,
            Count = quantity
        };

        Reactivity.GetInstance().InvokeTaskGraph();
        return _database.TaskResources.Add(taskResource);
    }

    private Error? ResourceExclusiveAvailable(ProjectDTO projectDto, ResourceDTO resourceDto)
    {
        if (resourceDto.Quantity <= GetResourceTotalUses(resourceDto) && resourceDto.IsExclusive)
            return new Error("ArgumentError", "El recurso exclusivo está en uso y no quedan instancias");

        return null;
    }

    public int GetResourceTotalUses(ResourceDTO resourceDto)
    {
        var resourceDtoId = resourceDto.Id;
        var quantityRes = _database.TaskResources.Query(context =>
            context.TaskResource.Where(r => r.ResourceId == resourceDtoId)
                .Join(
                    context.ProjectTask.Where(t => t.State == TaskState.Planed || t.State == TaskState.Started),
                    tr => tr.ProjectTaskId,
                    t => t.Id,
                    (tr, t) => tr
                    )
            )
            .ToList()
            .Sum(tr => tr.Count);
        return quantityRes;
    }

    public Error? RemoveResourceFromTask(UserDTO applicant, ResourceAdder resourceData)
    {
        var (_, userError) = HasPermissionToEdit(applicant);

        if (userError != null) return userError;

        var existence = _database.Resources.Exists(r => r.Type == resourceData.ResourceDto.Type);
        if (!existence) return new Error("KeyDoesNotExist", "El tipo de recurso no existe");

        var resource = _database.Resources.FindBy(r =>
            r.Name == resourceData.ResourceDto.Name && r.Type == resourceData.ResourceDto.Type);
        if (resource == null) return new Error("KeyDoesNotExist", "El recurso no existe");

        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var (project, error) = projectService.GetProject(resourceData.ProjectDto.Name);
        if (project == null) return error;

        project = ProjectConstruct.ConstructDeep(project, _contextFactory.Generate());

        if (!project.TaskList.Exists(task => task.Title == resourceData.TaskDto.Title))
            return new Error("KeyDoesNotExist", "La tarea no existe");

        var task = project.TaskList.Find(task => task.Title == resourceData.TaskDto.Title);

        var errorRelation = RemoveResourceTaskRelation(project, task!, resource);
        if (errorRelation != null) return errorRelation;

        return null;
    }

    private Error? RemoveResourceTaskRelation(Project project, ProjectTask task, Resource resource)
    {
        if (project.ResourceRelation.TryGetValue(task.Title, out var resourceList))
        {
            var idx = resourceList.FindIndex(res => res.Item1.Id == resource.Id);
            if (idx == -1) return new Error("KeyDoesNotExist", "El recurso no está asignado a la tarea");

            var (resourceObj, quantityObj) = resourceList[idx];

            if (quantityObj > 1)
            {
                var priority = _database.TaskResources.GetById((resource.Id, task.Id))!.Priority;
                resourceList[idx] = (resourceObj, quantityObj - 1);
                _database.TaskResources.Update(new TaskResource { ResourceId = resource.Id, ProjectTaskId = task.Id, Count = quantityObj - 1, Priority = priority });
            }
            else
            {
                _database.TaskResources.Remove((resource.Id, task.Id));
                resourceList.RemoveAt(idx);
            }
        }
        else
        {
            return new Error("KeyDoesNotExist", "La tarea no tiene recursos asignados");
        }

        Reactivity.GetInstance().InvokeTaskGraph();
        return null;
    }

    public List<ResourceDTO> GetAllResourcesDto()
    {
        return _database.Resources.GetAll().Select(ResourceDTO.ToEntity).ToList();
    }

    public List<(Resource, int)> GetResourcesUsedByTask(Project project, ProjectTask taskDto)
    {
        var context = _contextFactory.Generate();

        var tempRes = context.Resource
            .Join(
                context.TaskResource.Where(tr => tr.ProjectTaskId == taskDto.Id),
                r => r.Id,
                tr => tr.ResourceId,
                (r, tr) => new { r, tr.Count, tr.Priority }
            )
            .OrderBy(tr => tr.Priority);

        var result = new List<(Resource, int)>();

        foreach (var tRes in tempRes)
        {
            result.Add((ResourceConstruct.Construct(tRes.r, context), tRes.Count));
        }

        return result;
    }

    public List<TaskResource> GetAllTaskResource()
    {
        return _database.TaskResources.GetAll();
    }
}
