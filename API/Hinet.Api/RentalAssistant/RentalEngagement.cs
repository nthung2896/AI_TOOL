using System.Globalization;
using System.Text.RegularExpressions;

namespace Hinet.Api;

public record RentalMetrics(int? Reactions, int? Comments, int? Shares);
public static class RentalEngagement
{
    public static bool ValidPostUrl(string? value) => RentalPostLinks.Normalize(value) is not null;
    public static bool BelongsToGroup(string postUrl, string groupUrl) => ValidPostUrl(postUrl) && RentalFacebook.ValidUrl(groupUrl, true) &&
        new Uri(postUrl).AbsolutePath.Split('/')[2].Equals(new Uri(groupUrl).AbsolutePath.Split('/')[2], StringComparison.OrdinalIgnoreCase);
    public static string Canonical(string value) => RentalPostLinks.Normalize(value) ?? throw new ArgumentException("Link bài viết không hợp lệ.");
    // Missing or hidden counters stay unknown. Never turn an absent label into a zero.
    public static RentalMetrics Parse(IEnumerable<string> labels)
    {
        int? Read(string names)
        {
            foreach (var text in labels)
            {
                var match = Regex.Match(text, @"(?:(?:" + names + @")\s*[:：]?\s*(?<n>\d[\d.,]*\s*[kKmM]?)|(?<n>\d[\d.,]*\s*[kKmM]?)\s*(?:" + names + @"))(?:\b|$)", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                var raw = match.Groups["n"].Value.Trim().ToLowerInvariant();
                var factor = raw.EndsWith('k') ? 1000 : raw.EndsWith('m') ? 1000000 : 1;
                raw = factor == 1 ? raw.Replace(",", "").Replace(".", "") : raw[..^1].Trim().Replace(',', '.');
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var count) && count >= 0 && count * factor <= int.MaxValue)
                    return (int)Math.Round(count * factor);
            }
            return null;
        }
        return new(Read("all reactions|reactions|cảm xúc|lượt thích|likes"), Read("comments|comment|bình luận"), Read("shares|share|lượt chia sẻ"));
    }
}
