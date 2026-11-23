using System.Text.RegularExpressions;

namespace Hinet.Api;

public sealed class RentalMedia(string directory)
{
    public const int MaxImages = 10;
    public const long MaxFileBytes = 8 * 1024 * 1024;
    public const long MaxRequestBytes = 85 * 1024 * 1024;
    public static bool ValidId(string? id) => id is not null && Regex.IsMatch(id, @"\A[a-f0-9]{32}\.(jpg|png|webp)\z");
    public string Resolve(string id)
    {
        if (!ValidId(id)) throw new ArgumentException("Ảnh không hợp lệ.");
        var path = Path.Combine(directory, id);
        if (!File.Exists(path)) throw new ArgumentException("Không tìm thấy ảnh. Hãy tải ảnh lên lại.");
        return path;
    }
    public void ValidateImages(IEnumerable<string> images)
    {
        var list = images.ToList();
        if (list.Count > MaxImages || list.Distinct().Count() != list.Count) throw new ArgumentException("Chọn tối đa 10 ảnh khác nhau.");
        foreach (var id in list) Resolve(id);
    }
    public async Task<List<string>> Upload(IFormFileCollection files, CancellationToken ct)
    {
        if (files.Count is < 1 or > MaxImages) throw new ArgumentException("Chọn từ 1 đến 10 ảnh JPG, PNG hoặc WebP.");
        Directory.CreateDirectory(directory);
        var saved = new List<string>();
        try
        {
            foreach (var file in files)
            {
                if (file.Length is <= 0 or > MaxFileBytes) throw new ArgumentException("Mỗi ảnh tối đa 8 MB.");
                await using var input = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await input.CopyToAsync(buffer, ct);
                var bytes = buffer.ToArray();
                var extension = bytes.Length >= 12 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff ? "jpg"
                    : bytes.Length >= 12 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) ? "png"
                    : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? "webp" : null;
                if (extension is null) throw new ArgumentException("Chỉ nhận ảnh JPG, PNG hoặc WebP.");
                var id = Guid.NewGuid().ToString("N") + "." + extension;
                saved.Add(id);
                await File.WriteAllBytesAsync(Path.Combine(directory, id), bytes, ct);
            }
            return saved;
        }
        catch { foreach (var id in saved) File.Delete(Path.Combine(directory, id)); throw; }
    }
    public static string ContentType(string id) => Path.GetExtension(id) switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
}
