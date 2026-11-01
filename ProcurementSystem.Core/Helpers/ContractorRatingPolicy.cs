namespace ProcurementSystem.Core.Helpers;

public static class ContractorRatingPolicy
{
    public static string Grade(decimal score) => score switch
    {
        >= 85m => "A", >= 70m => "B", >= 50m => "C", _ => "D"
    };

    public static string GradeName(decimal score) => Grade(score) switch
    {
        "A" => "Hạng A - Xuất sắc", "B" => "Hạng B - Đạt yêu cầu",
        "C" => "Hạng C - Cần cải thiện", _ => "Hạng D - Cảnh báo rủi ro"
    };
}
