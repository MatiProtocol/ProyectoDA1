using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace DBContext;

public class SqlContextFactory : IDesignTimeDbContextFactory<SqlContext>, ISqlContextFactory
{
    public SqlContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(System.AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<SqlContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new SqlContext(optionsBuilder.Options);
    }

    public SqlContext Generate() => CreateDbContext([]);
}
