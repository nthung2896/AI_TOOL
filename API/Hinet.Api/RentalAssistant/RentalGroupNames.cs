using System.Text.RegularExpressions;
namespace Hinet.Api;
public static class RentalGroupNames
{
    public static string Clean(string value)
    {
        var text = value.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
        text = Regex.Replace(text, @"^(?:(?:profile|cover) (?:photo|picture) of\s+|ảnh (?:đại diện|bìa)(?: của)?\s+)", "", RegexOptions.IgnoreCase);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
    public static bool Useful(string value) => value.Length > 2 && !new[] { "join", "join group", "visit group", "view group", "tham gia", "tham gia nhóm", "xem nhóm" }.Contains(value.ToLowerInvariant());
    public static List<RentalGroup> CleanGroups(List<RentalGroup> groups)
    {
        foreach (var group in groups) group.Name = Clean(group.Name);
        return groups;
    }
}
