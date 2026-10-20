namespace DBContext;

public interface ISqlContextFactory
{
    public SqlContext Generate();
}