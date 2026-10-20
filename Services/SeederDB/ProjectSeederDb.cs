using DBContext;
using Domain;

namespace Services.SeederDB;

public abstract class ProjectSeederDb : ISeeder
{

    public static void Seed (ISqlContextFactory contextFactory)
    {
        var context = contextFactory.Generate(); 
        if (context.Project.Any()) return;
        
        var projects = new List<Project>();
        
        for (var i = 1; i <= 10; i++)
        {
            var project = new Project($"Project-{i}", $"Description for Project {i}")
            {
                StartDate = DateTime.Today.AddDays(i * 3),
                EndDate = DateTime.Today.AddDays(i * 3 + 10)
            };
            projects.Add(project);
        }
        
        var testProject = new Project("Proyiuto", "Descricricri")
        {
            StartDate = DateTime.Today.AddDays(1 * 3),
            EndDate = DateTime.Today.AddDays(7 * 3 + 10)
        };
        
        projects.Add(testProject);
        
        context.Project.AddRange(projects);
        
        context.SaveChanges();
    }
    
}