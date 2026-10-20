using Auxiliaries;

namespace Domain.HelpersDB;

public class ProjectUser
{
    public Roles Role { get; set; }
    
    public int ProjectId { get; set; }
    
    public int UserId { get; set; }
}