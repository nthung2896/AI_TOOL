namespace Hinet.Api;

public record GroupDiscoverySnapshot(string State, string Message, string Category, IReadOnlyList<RentalGroup> Results);
public sealed class RentalGroupDiscovery
{
    public static readonly IReadOnlyDictionary<string, string> Categories = new Dictionary<string, string>
    { ["rooms"] = "phòng trọ", ["realestate"] = "bất động sản", ["education"] = "giáo dục", ["other"] = "" };
    private readonly object gate = new();
    private string state = "Idle", message = "", category = "rooms", query = "";
    private List<RentalGroup> results = new();
    public GroupDiscoverySnapshot Snapshot() { lock (gate) return new(state, message, category, results.ToList()); }
    public void Start(string selectedCategory, string location, string keyword)
    {
        if (!Categories.TryGetValue(selectedCategory, out var term)) throw new ArgumentException("Danh mục không hợp lệ.");
        var text = string.Join(" ", new[] { term, location.Trim(), keyword.Trim() }.Where(v => !string.IsNullOrWhiteSpace(v)));
        if (text.Length is < 2 or > 250) throw new ArgumentException("Nhập từ khóa tìm nhóm từ 2 đến 250 ký tự.");
        lock (gate)
        {
            if (state is "Queued" or "Running") throw new InvalidOperationException("Đang tìm nhóm. Chờ kết quả trước khi tìm tiếp.");
            query = text; category = selectedCategory; state = "Queued"; message = "Đang chờ trình duyệt Facebook…"; results = new();
        }
    }
    public async Task<bool> Tick(RentalFacebook browser)
    {
        string text, selected;
        lock (gate) { if (state != "Queued") return false; text = query; selected = category; state = "Running"; message = "Đang tìm nhóm trên Facebook…"; }
        try
        {
            var found = await browser.SearchGroups(text);
            foreach (var group in found) { group.Category = selected; group.Enabled = false; }
            lock (gate) { results = found; state = "Completed"; message = found.Count == 0 ? "Chưa tìm thấy nhóm trong kết quả hiển thị. Thử địa điểm hoặc từ khóa khác." : $"Tìm thấy {found.Count} nhóm. Chọn nhóm muốn thêm vào danh sách."; }
        }
        catch (Exception) { lock (gate) { state = "Failed"; message = "Chưa tìm được nhóm. Kiểm tra đã đăng nhập Facebook trong Chromium và thử lại."; } }
        return true;
    }
}
