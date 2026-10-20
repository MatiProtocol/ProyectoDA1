using Auxiliaries;
using DBContext;
using Domain;
using Domain.HelpersDB;

namespace Services.SeederDB;

public class TaskRelationsDbSeeder : ISeeder
{
    public static void Seed(ISqlContextFactory contextFactory)
    {
        var context = contextFactory.Generate(); 
        if (!context.Project.Any() || context.ProjectTask.Any()) return;

        var projects = context.Project.ToList();

        foreach (var project in projects)
        {
            AddDefaultTasksToProject(project);
            context.ProjectTask.AddRange(project.TaskList);

            context.SaveChanges();
            
            AddTaskDependency(context, project);
            
            context.SaveChanges();
        }

        context.SaveChanges();
    }

    private static void AddDefaultTasksToProject(Project project)
    {
        project.TaskList = new List<ProjectTask>();
        var taskA = new ProjectTask("TaskA", "TaskA description", 1);
        var taskB = new ProjectTask("TaskB", "TaskB description", 1);

        project.AddTaskToList(taskA);
        project.AddTaskToList(taskB);
        taskA.ProjectId = project.Id;
        taskB.ProjectId = project.Id;
    }

    private static void AddTaskDependency(SqlContext context, Project project)
    {
        var taskA = project.TaskList[0];
        var taskB = project.TaskList[1];

        var taskToTask = new TaskToTask
        {
            ProjectTaskId = taskA.Id,
            DependentTaskId = taskB.Id,
            TypeDependency = TypeDependency.FS
        };

        context.TaskToTask.Add(taskToTask);
    }
}