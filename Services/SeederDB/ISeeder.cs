using DBContext;

namespace Services.SeederDB;

public interface ISeeder
{ 
    static abstract void Seed(ISqlContextFactory contextFactory);
}