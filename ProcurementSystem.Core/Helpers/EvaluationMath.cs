namespace ProcurementSystem.Core.Helpers;

public static class EvaluationMath
{
    public static decimal Normalize(decimal score, decimal maxScore) =>
        maxScore > 0 ? score / maxScore * 100m : 0m;

    public static decimal Weighted(decimal score, decimal maxScore, decimal weight, decimal totalWeight = 100m) =>
        totalWeight > 0 ? Normalize(score, maxScore) * weight / totalWeight : 0m;
}
