using Domain;
using Domain.HelpersDB;
using Microsoft.EntityFrameworkCore;

namespace DBContext;

public class SqlContext : DbContext
{
    public DbSet<Notification> Notification { get; set; }
    public DbSet<Project> Project { get; set; }
    public DbSet<ProjectTask> ProjectTask { get; set; }
    public DbSet<Resource> Resource { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<TaskResource> TaskResource { get; set; }
    public DbSet<TaskUser> TaskUser { get; set; }
    public DbSet<TaskToTask> TaskToTask { get; set; }
    public DbSet<ProjectUser> ProjectUser { get; set; }
    public DbSet<ProjectResourceExclusive> ProjectResourceExclusive { get; set; }
    
    public SqlContext(DbContextOptions<SqlContext> options) : base(options)
    {
        if (!Database.IsInMemory()) Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<TaskGraph>();
        modelBuilder.Ignore<ProjectTeam>();

        modelBuilder.Entity<Project>().HasKey(p => p.Id);
        modelBuilder.Entity<Project>().HasIndex(p => p.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();


        modelBuilder.Entity<TaskResource>().HasKey(tr => new { tr.ResourceId, tr.ProjectTaskId });

        modelBuilder.Entity<TaskResource>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(tr => tr.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskResource>()
            .HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(tr => tr.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<TaskUser>().HasKey(tu => new { tu.ProjectTaskId, tu.UserId });

        modelBuilder.Entity<TaskUser>()
            .HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(tu => tu.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskUser>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(tu => tu.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<TaskToTask>()
            .HasKey(t => new { t.ProjectTaskId, t.DependentTaskId });

        modelBuilder.Entity<TaskToTask>()
            .HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(t => t.ProjectTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskToTask>()
            .HasOne<ProjectTask>()
            .WithMany()
            .HasForeignKey(t => new { t.DependentTaskId })
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<ProjectUser>().HasKey(pu => new { pu.UserId, pu.ProjectId });

        modelBuilder.Entity<ProjectUser>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pu => pu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectUser>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(pu => pu.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<ProjectResourceExclusive>()
            .HasKey(er => new { er.ProjectId, er.ResourceId });

        modelBuilder.Entity<ProjectResourceExclusive>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(er => er.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectResourceExclusive>()
            .HasOne<Resource>()
            .WithMany()
            .HasForeignKey(er => er.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<ProjectTask>().HasKey(p => p.Id);
        modelBuilder.Entity<ProjectTask>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProjectTask>().HasIndex(t => new { t.Title, t.ProjectId }).IsUnique();

        
        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        modelBuilder.Entity<Project>().Ignore(p => p.ResourceRelation);
        modelBuilder.Entity<Project>().Ignore(p => p.ResourceExclusive);
        modelBuilder.Entity<Project>().Ignore(p => p.UserToTask);
        modelBuilder.Entity<Project>().Ignore(p => p.TaskToUser);
        modelBuilder.Entity<Project>().Ignore(p => p.Team);
        
        modelBuilder.Entity<Notification>().Ignore(p => p.UserToNotify);
        
        modelBuilder.Entity<Resource>().Ignore(p => p.Queue);
    }
}