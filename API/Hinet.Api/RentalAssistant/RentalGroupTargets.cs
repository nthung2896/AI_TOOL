using System.Text.Json;
namespace Hinet.Api;
public static class RentalGroupTargets
{
    public static string Serialize(List<Guid> ids) => JsonSerializer.Serialize(ids);
    public static List<Guid> Deserialize(string text) => JsonSerializer.Deserialize<List<Guid>>(text) ?? new();
    public static List<RentalGroup> Select(IEnumerable<RentalGroup> groups, IReadOnlyCollection<Guid>? ids)
    {
        var available = groups.Where(g => g.Enabled).ToList();
        if (ids is null) return available;
        var distinct = ids.Distinct().ToList();
        var selected = available.Where(g => distinct.Contains(g.Id)).ToList();
        if (selected.Count != distinct.Count) throw new InvalidOperationException("Có nhóm chưa được lưu hoặc đã tắt cho phép đăng. Chọn lại nhóm nhận bài.");
        return selected;
    }
}
