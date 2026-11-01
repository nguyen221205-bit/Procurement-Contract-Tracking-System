using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.Core.DTOs.Evaluation;

public class ReplaceCriteriaRequest
{
    [Required, MinLength(1), MaxLength(50)]
    public List<CreateCriteriaRequest> Items { get; set; } = new();

    // Prevent a template selected from an old screen from overwriting newer edits.
    [Required]
    public List<ExpectedCriteriaDto> ExpectedCriteria { get; set; } = new();
}

public class ExpectedCriteriaDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public bool IsPriceCriteria { get; set; }
}
