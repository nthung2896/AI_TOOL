namespace Hinet.Api;

public record GroupDiscoverySnapshot(string State, string Message, string Category, IReadOnlyList<RentalGroup> Results);
public sealed class RentalGroupDiscovery
{
    public static readonly IReadOnlyDictionary<string, string> Categories = new Dictionary<string, string>
    { ["rooms"] = "phòng trọ", ["realestate"] = "bất động sản", ["education"] = "giáo dục", ["other"] = "" };
    private readonly object gate = new();
    private string state = "Idle", message = "", category = "rooms";
    private List<string> queries = new();
    private List<RentalGroup> results = new();
    public GroupDiscoverySnapshot Snapshot() { lock (gate) return new(state, message, category, results.ToList()); }
    public void Start(string selectedCategory, string location, string keyword)
    {
        var searchTerms = BuildQueries(selectedCategory, location, keyword);
        lock (gate)
        {
            if (state is "Queued" or "Running") throw new InvalidOperationException("Đang tìm nhóm. Chờ kết quả trước khi tìm tiếp.");
            queries = searchTerms; category = selectedCategory; state = "Queued"; message = $"Đang chờ tìm {queries.Count} cụm từ trên Facebook…"; results = new();
        }
    }
    public async Task<bool> Tick(RentalFacebook browser)
    {
        List<string> terms; string selected;
        lock (gate) { if (state != "Queued") return false; terms = queries.ToList(); selected = category; state = "Running"; message = "Đang tìm nhóm trên Facebook…"; }
        var found = new Dictionary<string, RentalGroup>();
        var failures = 0;
        var processed = 0;
        for (var i = 0; i < terms.Count; i++)
        {
            lock (gate) message = $"Đang tìm {i + 1}/{terms.Count}: {terms[i]}";
            try
            {
                foreach (var group in await browser.SearchGroups(terms[i]))
                {
                    group.Category = selected; group.Enabled = false;
                    found.TryAdd(RentalFacebook.CanonicalUrl(group.Url), group);
                }
            }
            catch (Exception) { failures++; }
            processed++;
            if (found.Count >= 50) break;
        }
        lock (gate)
        {
            results = found.Values.Take(50).ToList();
            state = failures == terms.Count ? "Failed" : "Completed";
            message = failures == terms.Count ? "Chưa tìm được nhóm. Kiểm tra đăng nhập Facebook trong Chromium và thử lại."
                : $"Tìm thấy {results.Count} nhóm, đã tìm {processed}/{terms.Count} cụm từ và gộp link trùng." + (failures > 0 ? $" {failures} lượt chưa hoàn tất; có thể thử lại." : " Chọn nhóm muốn lưu.");
        }
        return true;
    }
    public static List<string> BuildQueries(string selectedCategory, string location, string keyword)
    {
        if (!Categories.TryGetValue(selectedCategory, out var term)) throw new ArgumentException("Danh mục không hợp lệ.");
        var phrases = keyword.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (phrases.Count > 5) throw new ArgumentException("Chọn tối đa 5 cụm từ cho mỗi lần tìm.");
        if (phrases.Count == 0) phrases.Add("");
        var result = phrases.Select(phrase => string.Join(" ", new[] { term, location.Trim(), phrase }.Where(v => !string.IsNullOrWhiteSpace(v)))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (result.Any(text => text.Length is < 2 or > 250)) throw new ArgumentException("Mỗi cụm tìm kiếm cần từ 2 đến 250 ký tự, tính cả khu vực.");
        return result;
    }
}
