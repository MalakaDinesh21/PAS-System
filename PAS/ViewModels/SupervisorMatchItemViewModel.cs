namespace PAS.ViewModels;

public class SupervisorMatchItemViewModel
{
    public int ProjectId { get; set; }
    public string? Title { get; set; }
    public string? ResearchArea { get; set; }
    public string? TechStack { get; set; }
    public string? Status { get; set; }

    public string StudentEmail { get; set; } = string.Empty;
}
