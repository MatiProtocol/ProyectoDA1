using Microsoft.EntityFrameworkCore;

namespace DBContext;

public class InMemorySqlContextFactory : ISqlContextFactory
{
    private string _name;

    public InMemorySqlContextFactory(string name)
    {
        _name = name;
    }
    public static SqlContext Create(string dbName = null!)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlContext>();
        optionsBuilder.UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString());

        return new SqlContext(optionsBuilder.Options);
    }

    public SqlContext Generate()
    {
        return Create(_name);
    }
}
