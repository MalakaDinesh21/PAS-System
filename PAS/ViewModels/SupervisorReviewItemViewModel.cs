namespace PAS.ViewModels;

public class SupervisorReviewItemViewModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? TechStack { get; set; }
    public string? ResearchArea { get; set; }
    public string? Status { get; set; }

    public bool HasExpressedInterest { get; set; }
}
