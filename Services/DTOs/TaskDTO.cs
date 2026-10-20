using Domain;
using Auxiliaries;
namespace Services.DTOs;

public class TaskDTO
{
    private int _id;
    public string Title;
    public string Description;
    public int DurationHour;
    public DateTime MinStartTaskDate;
    public int Slack;
    public DateTime EndTaskDate;
    public DateTime StartTaskDate;
    public TaskState State;
    private int _projectId;

    public (ProjectTask?, Error?) FromEntity()
    {
        try
        {
            var task = new ProjectTask(Title, Description, DurationHour)
            {
                Id = Id,
                ProjectId = ProjectId,
                State = State
            };
            return (task, null);
        }
        catch (Exception e)
        {
            return (null, new Error("ArgumentError", e.Message));
        }
    }

    public static TaskDTO ToEntity(ProjectTask task)
    {
        var taskDto = new TaskDTO
        {
            Title = task.Title,
            Description = task.Description,
            DurationHour = task.DurationHours,
            MinStartTaskDate = task.MinStartTaskDate,
            StartTaskDate = task.StartTaskDate,
            Slack = task.Slack,
            State = task.State,
            _projectId = task.ProjectId,
            EndTaskDate = task.EndTaskDate,
            _id = task.Id
        };
        return taskDto;
    }

    public int ProjectId
    {
        get => _projectId;
    }
    public int Id
    {
        get => _id;
    }
}
