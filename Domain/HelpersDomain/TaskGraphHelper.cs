using System.Diagnostics;
using Auxiliaries;

namespace Domain.HelpersDomain;

public class TaskGraphHelper
{
    public static Error? Add(TaskGraph taskGraph, ProjectTask t)
    {
        if (Has(taskGraph, t)) return new Error(Error.Types.KeyAlreadyExist, "The Task are already");

        var graph = taskGraph.Graph;
        var depGraph = taskGraph.DepGraph;
        var nameToTask = taskGraph.NameToTask;

        graph.Add(t.Title, new List<(TypeDependency type, ProjectTask task)>());
        depGraph.Add(t.Title, new List<(TypeDependency type, ProjectTask task)>());
        nameToTask.Add(t.Title, t);

        return null;
    }

    public static void AddDependencyToTask(TaskGraph taskGraph, ProjectTask target, ProjectTask dep, TypeDependency type)
    {
        var graph = taskGraph.Graph;
        var depGraph = taskGraph.DepGraph;

        var targetDepArray = graph[target.Title];
        targetDepArray.Add((type, dep));

        var inverseDepArray = depGraph[dep.Title];
        inverseDepArray.Add((type, target));
    }

    public static bool Has(TaskGraph taskGraph, ProjectTask t)
    {
        var graph = taskGraph.Graph;
        var depGraph = taskGraph.DepGraph;
        var exist = graph.ContainsKey(t.Title);

        Debug.Assert(exist == depGraph.ContainsKey(t.Title));
        return exist;
    }
}
