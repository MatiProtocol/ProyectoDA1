using DBContext;

namespace Services.SeederDB;

public abstract class SeedAll : ISeeder
{
    public static void Seed(ISqlContextFactory contextFactory)
    {
        UserSeederDb.Seed(contextFactory);
        ProjectSeederDb.Seed(contextFactory);
        ResourceSeederDb.Seed(contextFactory);
        NotificationSeederDb.Seed(contextFactory);
        TaskRelationsDbSeeder.Seed(contextFactory);
    }
}