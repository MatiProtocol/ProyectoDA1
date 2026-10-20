using System.Security.Cryptography;
using System.Text;
using Auxiliaries;
using DBContext;
using Domain;

namespace Services.SeederDB;

public abstract class UserSeederDb : ISeeder
{
    public static void Seed(ISqlContextFactory contextFactory)
    {
        var context = contextFactory.Generate();
        if (context.Users.Any()) return;

        var users = new List<User>
        {
            CreateUser("root", "rootiño", "root@root.com", new DateTime(1998, 3, 12), "PassRoot1@"),
            CreateUser("Ana", "Gonzalez", "ana@example.com", new DateTime(1998, 3, 12), "password`123"),
            CreateUser("Luis", "Martinez", "luis@example.com", new DateTime(1997, 6, 21), "pasS456!"),
            CreateUser("Carla", "Ramirez", "carla@example.com", new DateTime(1999, 2, 1), "abcD1234+"),
            CreateUser("testin", "testiño", "test@test.com", new DateTime(2002, 4, 18), "123456Ac@"),
            
            CreateUser("Mateo", "Fernandez", "mateo@example.com", new DateTime(2000, 10, 11), "my2323<>Pass"),
            CreateUser("Matias", "Fernandez", "Matias@example.com", new DateTime(2001, 12, 11), "my2323<>Pass"),
            CreateUser("Pedro", "Fernandez", "Pedro@example.com", new DateTime(1999, 03, 28), "my2323<>Pass"),
            CreateUser("Manuel", "Fernandez", "Manuel@example.com", new DateTime(2003, 02, 01), "my2323<>Pass"),
            CreateUser("Lucia", "Lopez", "lucia@example.com", new DateTime(1996, 7, 5), "secure.Pwd123"),
            CreateUser("Sofia", "Perez", "sofia@example.com", new DateTime(1995, 12, 23), "hellO/.123"),
            CreateUser("Julian", "Diaz", "julian@example.com", new DateTime(1998, 4, 17), "jul@Pass1212"),
            CreateUser("Valentina", "Moreno", "valentina@example.com", new DateTime(1997, 8, 14), "valeN@pwd2323"),
            CreateUser("Bruno", "Castro", "bruno@example.com", new DateTime(1996, 9, 9), "bruno@Sec123"),
            CreateUser("Maria", "Silva", "maria@example.com", new DateTime(1994, 11, 30), "Maria@321"),
            CreateUser( "System", "Manager", "test2@test.com", new DateTime(2002, 4, 18), "123456Ac@"),
            CreateUser("system", "Manager", "test3@test.com",new DateTime(2003, 4, 18), "123456Ac@"),
            CreateUser("system2", "Manager", "test4@test.com", new DateTime(2004, 4, 18), "123456Ac@")
        };

        users[0].Role = Roles.SystemManager;
        users[1].Role = Roles.ProjectLeader;
        users[2].Role = Roles.ProjectLeader;
        users[3].Role = Roles.ProjectLeader;
        users[15].Role = Roles.SystemManager;
        users[16].Role = Roles.ProjectManager;
        users[17].Role = Roles.ProjectLeader;
        context.Users.AddRange(users);
        context.SaveChanges();
    }

    private static User CreateUser(string name, string lastName, string email, DateTime birthDate, string password)
    {
        var hash = SHA256.Create();
        var passBytes = Encoding.UTF8.GetBytes(password);
        var passwordEncrypt = hash.ComputeHash(passBytes);
        return new User(name, lastName, email, birthDate, passwordEncrypt);
    }
}