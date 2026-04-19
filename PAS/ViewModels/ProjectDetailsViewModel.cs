using PAS.Models;

namespace PAS.ViewModels;

public class ProjectDetailsViewModel
{
    public required Project Project { get; set; }

    public string? SupervisorEmail { get; set; }
}
