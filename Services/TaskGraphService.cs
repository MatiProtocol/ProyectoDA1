using System.Diagnostics;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Domain.HelpersDomain;
using Services.DTOs;

namespace Services;

public class TaskGraphService
{
    public struct Edge
    {
        public TaskDTO targetDto;
        public TaskDTO depDto;
        public TypeDependency type;
    }

    private ISqlContextFactory _contextFactory;
    private TaskGraph _taskGraph;
    private Project _project;
    private PureDatabase _database;

    public TaskGraphService(Project project, ISqlContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
        _taskGraph = project.TaskGraph;
        _project = project;
        _database = PureDatabase.GetNewInstance(_contextFactory);
    }

    public Error? Add(ProjectTask t)
    {
        return TaskGraphHelper.Add(_taskGraph, t);
    }

    private int GetFirstIndex(List<(TypeDependency type, ProjectTask task)> list, ProjectTask task)
    {
        return list.FindIndex(tuple => tuple.task.Title == task.Title);
    }

    public void AddMany(ProjectTask[] ts)
    {
        foreach (var t in ts) Add(t);
    }

    public bool Remove(ProjectTask t)
    {

        var depGraph = _taskGraph.DepGraph;
        var graph = _taskGraph.Graph;
        var nameToTask = _taskGraph.NameToTask;

        if (!Has(t)) return false;
        foreach (var tupleTask in depGraph[t.Title])
        {
            var list = graph[tupleTask.task.Title];
            list.RemoveAt(GetFirstIndex(list, t));
        }
        foreach (var tupleTask in graph[t.Title])
        {
            var list = depGraph[tupleTask.task.Title];
            list.RemoveAt(GetFirstIndex(list, t));
        }
        graph.Remove(t.Title);
        depGraph.Remove(t.Title);
        nameToTask.Remove(t.Title);

        return true;
    }

    public void RemoveMany(ProjectTask[] ts)
    {
        foreach (var t in ts) Remove(t);
    }

    public bool HasMany(ProjectTask[] ts)
    {
        foreach (var t in ts)
        {
            if (!Has(t)) return false;
        }
        return true;
    }

    public void AddDependencyToTaskMany(ProjectTask target, ProjectTask[] deps, TypeDependency type)
    {
        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;

        Debug.Assert(graph[target.Title].All(value => deps.All(task => task.Title != value.task.Title)));
        Debug.Assert(Has(target) && HasMany(deps));
        Debug.Assert(VerifyValidNewEdges(target, deps));
        Debug.Assert(target.State != TaskState.Finished);

        _AddDependencyToTaskMany(target, deps, type);
    }

    private void _AddDependencyToTaskMany(ProjectTask target, ProjectTask[] deps, TypeDependency type)
    {
        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;

        foreach (var dep in deps)
        {
            var targetDepArray = graph[target.Title];
            targetDepArray.Add((type, dep));

            var inverseDepArray = depGraph[dep.Title];
            inverseDepArray.Add((type, target));
        }
    }

    public Error? AddDependencyToTask(ProjectTask target, ProjectTask dep, TypeDependency type)
    {
        if (!(Has(target) && Has(dep))) return new Error(Error.Types.KeyDoesNotExist, "The tasks must be added on the graph before adding a dependency");
        if (_taskGraph.Graph.TryGetValue(target.Title, out var value) && value.Any(t => t.task.Title == dep.Title && t.type == type))
            return new Error(Error.Types.KeyAlreadyExist, "The dependency already exists");

        Debug.Assert(_taskGraph.Graph[target.Title].All(value => dep.Title != value.task.Title));
        Debug.Assert(target.State != TaskState.Finished);

        var verifyNewEdge = VerifyValidNewEdge(target, dep);
        if (verifyNewEdge != null) return verifyNewEdge;

        _AddDependencyToTask(target, dep, type);
        return null;
    }

    public void _AddDependencyToTask(ProjectTask target, ProjectTask dep, TypeDependency type)
    {
        TaskGraphHelper.AddDependencyToTask(_taskGraph, target, dep, type);
    }

    public void RemoveDependencyFromTask(ProjectTask target, ProjectTask dep)
    {
        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;

        Debug.Assert(Has(target) && Has(dep));

        var targetDepArray = graph[target.Title]!;
        int index = GetFirstIndex(targetDepArray, dep);
        Debug.Assert(index != -1);
        targetDepArray.RemoveAt(index);

        var inverseDepArray = depGraph[dep.Title]!;
        inverseDepArray.RemoveAt(GetFirstIndex(inverseDepArray, target));
    }

    public void RemoveDependencyFromTaskMany(ProjectTask target, ProjectTask[] deps)
    {

        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;
        Debug.Assert(Has(target) && HasMany(deps));

        foreach (var dep in deps)
        {
            var targetDepArray = graph[target.Title]!;
            targetDepArray.RemoveAt(GetFirstIndex(targetDepArray, dep));

            var inverseDepArray = depGraph[dep.Title]!;
            inverseDepArray.RemoveAt(GetFirstIndex(inverseDepArray, target));
        }
    }

    public (TypeDependency type, ProjectTask task)[] GetTaskDependencies(ProjectTask t)
    {
        var graph = _taskGraph.Graph;

        Debug.Assert(Has(t));
        return graph[t.Title].ToArray();
    }

    public bool Has(ProjectTask t)
    {
        return TaskGraphHelper.Has(_taskGraph, t);
    }

    public void UpdateTitle(string title, ProjectTask t)
    {
        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;
        var nameToTask = _taskGraph.NameToTask;

        Debug.Assert(depGraph.ContainsKey(title) && !depGraph.ContainsKey(t.Title));
        var targetDeps = graph[title];
        var taskDepsOfTarget = depGraph[title];

        foreach (var deps in targetDeps)
        {
            foreach (var t1 in depGraph[deps.task.Title])
            {
                if (t1.task.Title == title) t1.task.Title = t.Title;
            }
        }

        graph.Remove(title);
        depGraph.Remove(title);
        nameToTask.Remove(title);

        graph.Add(t.Title, targetDeps);
        depGraph.Add(t.Title, taskDepsOfTarget);
        nameToTask.Add(t.Title, t);
    }

    public (ProjectTask[] path, int duration) GetCriticalPath()
    {
        if (_taskGraph.NameToTask.Count == 0) return ([], 0);

        CalculateEstimations();

        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;
        var path = new List<ProjectTask>();
        var endOfGraph = _taskGraph.EndOfGraph;
        var current = endOfGraph;
        Debug.Assert(Has(current));

        var changed = true;

    search: do
        {
            changed = false;

            path.Add(current);

            var listSS = new List<ProjectTask>();

            var minSlack = new ProjectTask("Esto no deberia estar aca, deberia ser cambiado en le siguiente foreach", "", 0);
            ;
            minSlack.Slack = int.MaxValue;
            foreach (var t in graph[current.Title])
            {
                if (t.type == TypeDependency.SS)
                {
                    listSS.Add(t.task);
                    continue;
                }

                if (t.task.Slack == 0)
                {
                    current = t.task;

                    changed = true;
                    goto search;
                }
                if (minSlack.Slack > t.task.Slack)
                    minSlack = t.task;
            }

            if (!changed && listSS.Count > 0)
            {
                var minStartDate = new ProjectTask("Esto no deberia estar aca, deberia ser cambiado en le siguiente foreach", "", 0);
                ;
                minStartDate.MinStartTaskDate = DateTime.MaxValue;
                foreach (var t in listSS)
                {
                    if (current.MinStartTaskDate.CompareTo(t.MinStartTaskDate) == 0)
                    {
                        current = t;
                        changed = true;
                        goto search;
                    }

                    if (minStartDate.MinStartTaskDate > t.MinStartTaskDate)
                        minSlack = t;
                }

                current = minStartDate;
                changed = minStartDate.MinStartTaskDate != DateTime.MaxValue;
            }
            else
            {
                current = minSlack;
                changed = minSlack.Slack != int.MaxValue;
            }

        } while (
            changed &&
            (graph[current.Title].Count > 0 ||
             depGraph[current.Title].Count > 0)
        );

        path.Reverse();
        var lastTask = path[path.Count - 1];

        if (lastTask.State == TaskState.Finished) return (path.ToArray(), 0);

        var duration = _taskGraph.TimeTable[lastTask.Title] + lastTask.DurationHours;
        return (path.ToArray(), (int)(duration / 8) * 24 + duration % 8);
    }

    private void UpdateEnfOfGraph(ProjectTask task)
    {
        var durationNew = task.DurationHours;
        var finalDate =
            (task.State == TaskState.Planed || task.State == TaskState.Started
                ? task.StartTaskDate
                : task.MinStartTaskDate).AddDays((int)(durationNew / 8)).AddHours(durationNew % 8);

        var endGraph = _taskGraph.EndOfGraph;
        var durationEnd = endGraph.DurationHours;
        if ((endGraph.State == TaskState.Planed || endGraph.State == TaskState.Started
                ? endGraph.StartTaskDate
                : endGraph.MinStartTaskDate).AddDays((int)(durationEnd / 8)).AddHours(durationEnd % 8).CompareTo(finalDate) < 0)
        {
            _taskGraph.EndOfGraph = task;
        }
    }

    private void ResetTaskEstimation()
    {
        _taskGraph.EndOfGraph = new ProjectTask("Esto no deberia estar aca, deberia ser cambiado al llamar CalculateEstimation", "", 0);

        var timeTable = _taskGraph.TimeTable;

        foreach (var task in _taskGraph.NameToTask)
        {
            if (!timeTable.ContainsKey(task.Key)) timeTable.Add(task.Key, 0);
            if (task.Value.State == TaskState.Finished) continue;
            task.Value.Slack = int.MaxValue;
            if (task.Value.State != TaskState.Pending) continue;

            timeTable[task.Key] = 0;
        }
    }

    public void CalculateEstimations()
    {
        CalculateEstimations(GetDateTimeNow());
    }

    public void CalculateEstimations(DateTime init)
    {
        if (_taskGraph.NameToTask.Count == 0) return;

        var graph = _taskGraph.Graph;
        var depGraph = _taskGraph.DepGraph;
        var nameToTask = _taskGraph.NameToTask;

        ResetTaskEstimation();

        var l = new List<ProjectTask>();

        foreach (var t in graph)
        {
            if (depGraph[t.Key].Count > 0) continue;
            var key = nameToTask[t.Key];

            CalculateEstimationsMin(init, key);

            l.Add(key);

            UpdateEnfOfGraph(key);
        }
        var endOfGraph = _taskGraph.EndOfGraph;
        var durationWithOutBreak = _taskGraph.TimeTable[endOfGraph.Title] + endOfGraph.DurationHours;


        foreach (var t in l)
        {
            CalculateEstimationsMax(t, durationWithOutBreak, durationWithOutBreak, TypeDependency.FS);
        }

        foreach (var task in _taskGraph.NameToTask)
        {
            _database.Tasks.Update(task.Value, false);
        }
    }

    private DateTime _delayTimeEstimationForResource(DateTime init, ProjectTask t)
    {

        var list = ResourceService.GetNewInstance(_contextFactory).GetResourcesUsedByTask(_project, t);
        DateTime max = init;
        foreach (var res in list)
        {
            var available = res.Item1.Quantity - ResourceService.GetNewInstance(_contextFactory).GetResourceTotalUses(ResourceDTO.ToEntity(res.Item1));

            var filteredQueue = res.Item1.Queue.Where(x => x.task.State != TaskState.Finished);
            var tasksOrderByEndDate = filteredQueue.ToList();

            var tasksOrderByStartDate = filteredQueue.ToList();

            var startIndex = 0;
            var endIndex = 0;
            var date = DateTime.MinValue;

            while ((startIndex < tasksOrderByStartDate.Count || endIndex < tasksOrderByEndDate.Count))
            {
                while (startIndex < tasksOrderByStartDate.Count && available >= tasksOrderByStartDate[startIndex].quantity && tasksOrderByStartDate[startIndex].task.Id != t.Id)
                {
                    var task = tasksOrderByStartDate[startIndex];

                    if (task.task.State != TaskState.Pending)
                    {
                        startIndex += 1;
                        continue;
                    }

                    date = task.task.MinStartTaskDate;
                    available -= task.quantity;
                    startIndex += 1;
                }

                if (tasksOrderByStartDate[startIndex].task.Id == t.Id && available >= res.Item2) break;

                if (date < tasksOrderByEndDate[endIndex].task.GetEndDate())
                {
                    date = tasksOrderByEndDate[endIndex].task.GetEndDate();
                    available += tasksOrderByEndDate[endIndex].quantity;
                    endIndex += 1;
                }
                
                if (tasksOrderByStartDate[startIndex].task.Id == t.Id && available >= res.Item2) break;

                while (endIndex < tasksOrderByEndDate.Count && date > tasksOrderByEndDate[endIndex].task.GetEndDate() && tasksOrderByEndDate[endIndex].task.Id != t.Id)
                {
                    date = tasksOrderByEndDate[endIndex].task.GetEndDate();
                    available += tasksOrderByEndDate[endIndex].quantity;
                    endIndex += 1;
                }

                if (endIndex < tasksOrderByEndDate.Count && tasksOrderByEndDate[endIndex].task.Id == t.Id) endIndex += 1;
                if (tasksOrderByStartDate[startIndex].task.Id == t.Id && available >= res.Item2) break;
            }

            if (max < date)
                max = date;
        }

        return max;
    }

    public static DateTime CalculateEndDate(DateTime start, int duration)
    {

        return start.AddDays(duration / 8).AddHours(duration % 8);
    }

    private int CalculateEstimationsMin(DateTime init, ProjectTask t)
    {
        var graph = _taskGraph.Graph;

        var durationT = t.DurationHours;
        if (t.State == TaskState.Finished) return -t.DurationHours;
        if (t.State == TaskState.Started || t.State == TaskState.Planed)
        {
            var diff = (int)Math.Round((t.StartTaskDate - t.MinStartTaskDate).TotalHours);
            diff = diff / 24 * 8 + Math.Min(diff % 24, 8);
            _taskGraph.TimeTable[t.Title] = diff;
            UpdateEnfOfGraph(t);
            return diff;
        }

        var taskWhichDependInT = graph[t.Title];
        if (taskWhichDependInT.Count == 0)
        {
            t.MinStartTaskDate = _delayTimeEstimationForResource(init, t);
            var diff1 = (int)Math.Round((t.MinStartTaskDate - init).TotalHours);
            _taskGraph.TimeTable[t.Title] = ((int)(diff1 / 24)) * 8 + Math.Min(diff1 % 24, 8);
            return _taskGraph.TimeTable[t.Title];
        }

        var listSS = new List<ProjectTask>();

        var maxLengthHour = 0;
        foreach (var tdep in taskWhichDependInT)
        {
            if (tdep.type == TypeDependency.SS)
            {
                listSS.Add(tdep.task);
                continue;
            }

            var minHours = CalculateEstimationsMin(init, tdep.task);

            var durationDep = tdep.task.DurationHours;
            minHours += durationDep;

            if (maxLengthHour < minHours)
                maxLengthHour = minHours;
        }

        foreach (var taskSS in listSS)
        {
            var min = CalculateEstimationsMin(init, taskSS);
            if (maxLengthHour < min) maxLengthHour = min;

            UpdateEnfOfGraph(taskSS);
        }

        var maxLengthDate = CalculateEndDate(init, maxLengthHour);

        t.MinStartTaskDate = _delayTimeEstimationForResource(maxLengthDate, t);
        var diff2 = (int)Math.Round((t.MinStartTaskDate - maxLengthDate).TotalHours);
        _taskGraph.TimeTable[t.Title] = maxLengthHour + diff2 / 24 * 8 + Math.Min(diff2 % 24, 8); ;

        return _taskGraph.TimeTable[t.Title];
    }

    public static DateTime TransformDateTimeToNothingLowerThanMinutes(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
    }

    public static DateTime GetDateTimeNow()
    {
        return TransformDateTimeToNothingLowerThanMinutes(DateTime.Now);
    }

    private void CalculateEstimationsMax(ProjectTask t, int max, int maxSS, TypeDependency type)
    {

        var graph = _taskGraph.Graph;

        if (t.State == TaskState.Finished) return;


        var newMax = (type == TypeDependency.SS ? maxSS : max) - t.DurationHours;

        var newSlack = newMax - _taskGraph.TimeTable[t.Title];

        if (t.Slack > newSlack) t.Slack = newSlack;
        if (t.State == TaskState.Started || t.State == TaskState.Planed) return;

        foreach (var tdep in graph[t.Title])
        {
            var useThisDate = newMax;
            if (tdep.type == TypeDependency.FS && type == TypeDependency.SS) useThisDate = _taskGraph.TimeTable[t.Title];
            CalculateEstimationsMax(tdep.task, tdep.type == TypeDependency.FS ? useThisDate : max, tdep.type == TypeDependency.FS ? useThisDate : maxSS, tdep.type);
        }

    }

    public Error? VerifyValidNewEdge(ProjectTask target, ProjectTask dep)
    {
        Debug.Assert(target.State != TaskState.Finished);
        var graph = _taskGraph.Graph;
        if (!Has(target) || !Has(dep)) return new Error(Error.Types.KeyDoesNotExist, "Las tareas tiene que estar ingresadas"); ;
        if (graph[target.Title].Count != 0 && graph[target.Title].Any(value => dep.Title == value.task.Title)) return new Error(Error.Types.KeyAlreadyExist, "No se puede verificar dependencias repetidas");

        _AddDependencyToTask(target, dep, TypeDependency.FS);

        var visited = new Dictionary<string, bool>();
        var stack = new Dictionary<string, bool>();

        foreach (var task in _taskGraph.NameToTask)
        {
            visited.Add(task.Key, false);
            stack.Add(task.Key, false);
        }

        var result = SearchCycleWithOneDep(target, visited, stack);

        RemoveDependencyFromTask(target, dep);

        if (result) return new Error(Error.Types.DefaultError, "La dependencia no se pudo agregar ya que genera un ciclo");
        return null;
    }

    private bool VerifyValidNewEdges(ProjectTask target, ProjectTask[] deps)
    {
        Debug.Assert(Has(target) && HasMany(deps));
        Debug.Assert(target.State != TaskState.Finished);
        var graph = _taskGraph.Graph;
        Debug.Assert(graph[target.Title].All(value => deps.All(task => task.Title != value.task.Title)));
        _AddDependencyToTaskMany(target, deps, TypeDependency.FS);

        var visited = new Dictionary<string, bool>();
        var stack = new Dictionary<string, bool>();

        foreach (var task in _taskGraph.NameToTask)
        {
            visited.Add(task.Key, false);
            stack.Add(task.Key, false);
        }

        var result = SearchCycleWithOneDep(target, visited, stack);

        RemoveDependencyFromTaskMany(target, deps);

        return !result;
    }


    private bool SearchCycleWithOneDep(ProjectTask current, Dictionary<string, bool> visited, Dictionary<string, bool> stack)
    {
        if (stack[current.Title])
            return true;

        if (visited[current.Title])
            return false;

        visited[current.Title] = true;
        stack[current.Title] = true;

        foreach (var task in _taskGraph.Graph[current.Title])
        {
            if (SearchCycleWithOneDep(task.task, visited, stack))
                return true;
        }

        stack[current.Title] = false;
        return false;
    }

    public bool ValidTaskToStart(ProjectTask t)
    {
        Debug.Assert(Has(t));

        var deps = _taskGraph.Graph[t.Title];

        if (deps.Count == 0) return true;

        foreach (var dep in deps)
        {
            if (dep.type == TypeDependency.FS && dep.task.State != TaskState.Finished) return false;
            if (dep.type == TypeDependency.SS && dep.task.State == TaskState.Pending) return false;
        }

        return true;
    }
}
