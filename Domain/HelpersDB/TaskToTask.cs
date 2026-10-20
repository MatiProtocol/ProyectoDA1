using Auxiliaries;

namespace Domain.HelpersDB;

public class TaskToTask
{
    public TypeDependency TypeDependency { get; set; }
    public int ProjectTaskId { get; set; }
    
    public int DependentTaskId { get; set; }
}