using System.Globalization;
using System.Text;

namespace Hinet.Api;

// Deterministic offline mode. Clearly identified as templates in the product UI.
public static class RentalTemplates
{
    public static string Draft(RentalRoom room) =>
        $"CHO THUÊ {room.Name.ToUpperInvariant()}\n\n" +
        $"Địa chỉ: {room.Address}\nGiá thuê: {room.MonthlyRent.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}đ/tháng\n\n" +
        $"{room.Details}\n\nBạn quan tâm hãy nhắn Messenger để hỏi thêm và đặt lịch xem phòng nhé!";

    public static RentalAnswer Answer(List<RentalRoom> rooms, string conversation)
    {
        var latest = conversation.Split('\n').LastOrDefault(x => x.StartsWith("Khách:", StringComparison.OrdinalIgnoreCase))?[6..].Trim() ?? conversation.Trim();
        var available = rooms.Where(r => r.Available).ToList();
        if (available.Count == 0) return new("Mình là trợ lý tự động. Hiện danh sách chưa có phòng còn trống; chủ nhà sẽ hỗ trợ bạn thêm nhé.", null);
        // Booking format is explicit, so offline mode never guesses identities or dates from prose.
        var fields = latest.Split('|', StringSplitOptions.TrimEntries);
        if (fields.Length == 5 && Normalize(fields[0]) == "hen xem")
        {
            var matches = available.Where(r => Normalize(r.Name) == Normalize(fields[1])).ToList();
            if (matches.Count == 1 && !string.IsNullOrWhiteSpace(fields[2]) && System.Text.RegularExpressions.Regex.IsMatch(fields[3], @"^(\+84|0)\d{9}$") &&
                DateTime.TryParseExact(fields[4], "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                var visit = new DateTimeOffset(time, TimeSpan.FromHours(7));
                if (visit > DateTimeOffset.UtcNow && visit < DateTimeOffset.UtcNow.AddDays(90))
                    return new("Mình đang kiểm tra lịch bạn yêu cầu.", new(matches[0].Id, fields[2], fields[3], visit));
            }
            return new("Bạn kiểm tra lại tên phòng, họ tên, số điện thoại Việt Nam và giờ xem trong 90 ngày tới, rồi gửi: Hẹn xem | Tên phòng | Họ tên | Số điện thoại | dd/MM/yyyy HH:mm. Nếu có nhiều phòng trùng tên, chủ nhà sẽ hỗ trợ chọn phòng cụ thể.", null);
        }
        var normalized = Normalize(latest);
        var selected = available.Where(r => normalized.Contains(Normalize(r.Name))).ToList();
        var relevant = selected.Count == 1 ? selected : available.Take(5).ToList();
        var details = string.Join("\n\n", relevant.Select(r => $"{r.Name}: {r.MonthlyRent.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))}đ/tháng\n{r.Address}\n{r.Details}"));
        return new($"Mình là trợ lý tự động theo thông tin chủ nhà cung cấp.\n\n{details}\n\n" +
            "Để hẹn xem, bạn gửi theo mẫu: Hẹn xem | Tên phòng | Họ tên | Số điện thoại | dd/MM/yyyy HH:mm. Ví dụ giờ: 25/12/2026 18:30. Nếu cần trao đổi khác, chủ nhà sẽ hỗ trợ bạn.", null);
    }
    public static string Normalize(string text) => new string(text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Normalize(NormalizationForm.FormC).Trim();
}
