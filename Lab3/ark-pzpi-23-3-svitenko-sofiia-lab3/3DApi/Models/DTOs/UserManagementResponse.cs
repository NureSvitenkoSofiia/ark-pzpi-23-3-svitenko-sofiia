namespace _3DApi.Models.DTOs;

public class UserManagementResponse
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int TotalJobsSubmitted { get; set; }
    public int CompletedJobs { get; set; }
    public DateTimeOffset? LastJobDate { get; set; }
}

