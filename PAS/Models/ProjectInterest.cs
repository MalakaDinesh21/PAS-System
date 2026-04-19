namespace PAS.Models;

public class ProjectInterest
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string SupervisorId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
