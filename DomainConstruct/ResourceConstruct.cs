using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using DBContext;
using Domain;

namespace DomainConstruct;

public class ResourceConstruct
{
    

    public static Resource Construct(Resource res, SqlContext context)
    {
        var queueQuery = context.Resource
            .Where(r => r.Id == res.Id)
            .Join(
                context.TaskResource,
                r => r.Id,
                tr => tr.ResourceId,
                (r, tr) => new { Resource = r, TaskResource = tr }
            )
            .OrderBy(t => t.TaskResource.Priority)
            .Select(t => new { t.TaskResource.ProjectTaskId, t.TaskResource.Count, t.TaskResource.Priority })
            .Join(
                context.ProjectTask,
                t => t.ProjectTaskId,
                t => t.Id,
                (t, tt) => new { tt, t.Count, t.Priority }
            );

        var queue = new List<(ProjectTask task, int, int)>();

        foreach (var tuple in queueQuery)
        {
            queue.Add((tuple.tt, tuple.Count, tuple.Priority));
        }

        res.Queue = queue;
        
        return res;
    }
}