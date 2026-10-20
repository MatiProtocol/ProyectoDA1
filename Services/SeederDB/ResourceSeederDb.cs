using DBContext;

namespace Services.SeederDB;
using Domain;

public abstract class ResourceSeederDb : ISeeder
{
    public static void Seed(ISqlContextFactory contextFactory)
    {
        var context = contextFactory.Generate(); 
        if (context.Resource.Any()) return;
        
        var resources = new List<Resource>
        {
            new Resource("Tablet", "Hardware", "iPad Pro for UI testing", 4),
            new Resource("Designer Account", "Software", "Adobe Creative Cloud license", 3),
            new Resource("Test Devices", "Hardware", "Mobile phones for QA testing", 6),
            new Resource("Projector", "Facility", "HD Projector in Room 204", 1),
            new Resource("Azure Credits", "Cloud", "Cloud budget for project deployment", 1000),
            new Resource("DevOps Tooling", "Software", "Jira and CI/CD tools", 10)
        };

        context.Resource.AddRange(resources);
        context.SaveChanges();
    }
}