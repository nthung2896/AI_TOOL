using System.Text.RegularExpressions;

namespace Hinet.Api;

public static class RentalTelegram
{
    public static RentalRoom? SelectRoom(IEnumerable<RentalRoom> rooms, string text)
    {
        var available = rooms.Where(r => r.Available).ToList();
        string Normalize(string value) => Regex.Replace(RentalTemplates.Normalize(value), "[^a-z0-9]+", " ").Trim();
        var message = " " + Normalize(text) + " ";
        var matches = available.Where(r => Normalize(r.Name).Length > 0 && message.Contains(" " + Normalize(r.Name) + " ")).ToList();
        if (matches.Count > 0) return matches.Count == 1 ? matches[0] : null;
        return available.Count == 1 ? available[0] : null;
    }
    public static string? InteractionNotice(string room, string group, string url, RentalMetrics before, RentalMetrics after)
    {
        var increases = new List<string>();
        void Add(string label, int? oldValue, int? newValue)
        {
            if (oldValue.HasValue && newValue.HasValue && newValue > oldValue)
                increases.Add($"{label}: +{newValue - oldValue} (tổng {newValue})");
        }
        Add("Cảm xúc", before.Reactions, after.Reactions); Add("Bình luận", before.Comments, after.Comments); Add("Chia sẻ", before.Shares, after.Shares);
        var title = before.Comments.HasValue && after.Comments > before.Comments ? "CÓ BÌNH LUẬN MỚI" : "CÓ TƯƠNG TÁC MỚI";
        return increases.Count == 0 ? null : $"{title}\n{room}\nNhóm: {group}\n{string.Join("\n", increases)}\n{url}";
    }
    public static List<InboxMessage> NewMessages(IReadOnlyList<InboxMessage> messages, string lastId, bool initialized, bool newConversation)
    {
        if (messages.Count == 0) return new();
        if (!initialized) return newConversation ? messages.TakeLast(1).ToList() : new();
        var index = messages.ToList().FindLastIndex(m => m.Id == lastId);
        return index >= 0 ? messages.Skip(index + 1).ToList() : messages.TakeLast(1).ToList();
    }
    public static string IncomingNotice(string customer, string url, IReadOnlyList<InboxMessage> messages)
    {
        var preview = string.Join("\n", messages.Select(m => m.Text));
        if (preview.Length > 2400) preview = preview[..2400] + "…";
        return $"TIN NHẮN MESSENGER MỚI\nKhách: {customer}\n{messages.Count} tin mới · Trích nội dung:\n{preview}\nMở hội thoại: {url}";
    }
}
