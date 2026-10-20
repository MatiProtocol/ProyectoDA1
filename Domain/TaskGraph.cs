using Auxiliaries;

namespace Domain;

public class TaskGraph
{
  private ProjectTask _endOfGraph =
    new ProjectTask("Esto no deberia estar aca, deberia ser cambiado al llamar CalculateEstimation", "", 0);

  private Dictionary<string, ProjectTask> _nameToTask;
  private Dictionary<string, int> _timeTable = new Dictionary<string, int>();
  private Dictionary<string, List<(TypeDependency type, ProjectTask task)>> _depGraph;
  private Dictionary<string, List<(TypeDependency type, ProjectTask task)>> _graph;

  public TaskGraph()
  {
    _nameToTask = new Dictionary<string, ProjectTask>();
    _graph = new Dictionary<string, List<(TypeDependency type, ProjectTask task)>>();
    _depGraph = new Dictionary<string, List<(TypeDependency type, ProjectTask task)>>();
  }
  
  public Dictionary<string, List<(TypeDependency type, ProjectTask task)>> DepGraph
  {
    get => _depGraph;
    set => _depGraph = value;
  }

  public Dictionary<string, List<(TypeDependency type, ProjectTask task)>> Graph
  {
    get => _graph;
    set => _graph = value;
  }

  public Dictionary<string, ProjectTask> NameToTask
  {
    get => _nameToTask;
    set => _nameToTask = value;
  }

  public ProjectTask EndOfGraph
  {
    get => _endOfGraph;
    set => _endOfGraph = value;
  }

  public Dictionary<string, int> TimeTable
  {
    get => _timeTable;
    set => _timeTable = value;
  }
}
