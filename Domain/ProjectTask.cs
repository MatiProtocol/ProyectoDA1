using System.Diagnostics;
using Auxiliaries;

namespace Domain;

public class ProjectTask
{

    private int _id;
    
    private string _title;
    private int _projectId;
    
    private string _description;
    private int _durationHour;

    private DateTime _minStartTaskDate;
    private int _slack;
    private DateTime _startTaskDate;
    private DateTime _endTaskDate;
    private TaskState _state;

    public ProjectTask() {}
    
    public ProjectTask(string title, string description, int durationHour)
    {
        if (ValidateTitle(title) == null) _title = title;
        else throw new ArgumentException("Invalid Title");
        _description = description;
        _durationHour = durationHour;
        _minStartTaskDate = DateTime.MinValue;
        _slack = int.MaxValue;
        _endTaskDate = DateTime.Today;
    }

    public int Id
    {
        get => _id;
        set => _id = value;
    }
    
    public int ProjectId
    {
        get => _projectId;
        set => _projectId = value;
    }
    
    public string Title 
    {
        get => _title;
        set
        {
            var error = ValidateTitle(value);
            if (error != null) throw new ArgumentException(error.GetMessage());
            _title = value;
        }
    }

    public string Description
    {
        get => _description;
        set => _description = value;
    }

    public int DurationHours
    {
        get => _durationHour;
        set => _durationHour = value;
    }

    public DateTime MinStartTaskDate
    {
        get => _minStartTaskDate;
        set => _minStartTaskDate = value;
    }

    public int Slack
    {
        get => _slack;
        set => _slack = value;
    }
    public TaskState State
    {
        get => _state;
        set => _state = value;
    }

    public DateTime EndTaskDate
    {
        get => _endTaskDate;
        set => _endTaskDate = value;
    }

    public void SetEndTaskDate(DateTime date)
    {
        _endTaskDate = date;
        State = TaskState.Finished;
    }

    public DateTime StartTaskDate
    {
        get => _startTaskDate;
        set => _startTaskDate = value;
    }

    public void SetPlanedTaskDate(DateTime date)
    {
        Debug.Assert(State != TaskState.Started);
        Debug.Assert(State != TaskState.Finished);
        _startTaskDate = date;
        State = TaskState.Planed;
    }


    public void SetStartTaskDate(DateTime date)
    {
        Debug.Assert(State != TaskState.Finished);
        _startTaskDate = date;
        State = TaskState.Started;
    }

    private Error? ValidateTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return new Error("ArgumentError", "El nombre de la tare no puede ser vacio");
        }

        return null;
    }

    public DateTime GetStartDate()
    {
        return State == TaskState.Planed || State == TaskState.Started ? StartTaskDate : MinStartTaskDate;
    }

    public DateTime GetEndDate()
    {
        return GetStartDate().AddDays((int)(DurationHours / 8)).AddHours(DurationHours % 8); ;
    }
}
