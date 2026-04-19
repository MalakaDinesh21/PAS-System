using System.ComponentModel.DataAnnotations;

namespace PAS.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? TechStack { get; set; }

        public string? ResearchArea { get; set; }

        public string? Status { get; set; } = "Pending";

        public string? StudentId { get; set; }

        public string? SupervisorId { get; set; }

        public DateTimeOffset? MatchedAt { get; set; }
    }
}