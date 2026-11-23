using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Hinet.Api;

public record MessengerSelectors(string IncomingMessages, string OutgoingMessages, string MessageIdAttribute);
public record MessengerSetupSnapshot(string State, string Message, bool Configured);

// All browser work is performed by the worker; HTTP handlers only queue setup requests.
public sealed class RentalMessengerSetup(IWebHostEnvironment environment, IOptions<RentalOptions> options)
{
    private readonly object gate = new();
    private string state = "Idle", message = "", url = "";
    private bool cancel;
    private DateTimeOffset started;
    public string SettingsPath => options.Value.MessengerSettingsPath ?? Path.Combine(environment.ContentRootPath, "App_Data", "messenger-selectors.json");
    public bool Configured => !string.IsNullOrWhiteSpace(options.Value.IncomingMessages) && !string.IsNullOrWhiteSpace(options.Value.OutgoingMessages);
    public MessengerSetupSnapshot Snapshot() { lock (gate) return new(state, message, Configured); }
    public void Start(string target)
    {
        if (!RentalFacebook.ValidUrl(target, false)) throw new ArgumentException("Nhập link https://www.facebook.com/messages/t/ID.");
        lock (gate)
        {
            if (state is "Queued" or "Running") throw new InvalidOperationException("Đang có phiên thiết lập. Hoàn tất hoặc hủy phiên hiện tại.");
            url = RentalFacebook.CanonicalUrl(target); state = "Queued"; message = "Đang chờ worker mở hội thoại…"; cancel = false;
        }
    }
    public void Cancel() { lock (gate) cancel = true; }
    private void Set(string next, string detail) { lock (gate) { state = next; message = detail; } }
    public async Task<bool> Tick(RentalFacebook browser, CancellationToken ct)
    {
        string current, target; bool cancelled;
        lock (gate) { current = state; target = url; cancelled = cancel; }
        if (current is not ("Queued" or "Running")) return false;
        try
        {
            if (cancelled || (current == "Running" && DateTimeOffset.UtcNow - started > TimeSpan.FromMinutes(5)))
            { await browser.EndMessengerSetup(); Set("Cancelled", "Đã kết thúc thiết lập. Có thể bắt đầu lại."); return false; }
            if (current == "Queued")
            {
                var script = await File.ReadAllTextAsync(Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "rental-desktop", "messenger-setup.js"), ct);
                await browser.BeginMessengerSetup(target, script);
                started = DateTimeOffset.UtcNow;
                Set("Running", "Trong Chromium: chọn một tin khách gửi, sau đó một tin bạn gửi. Không có tin nhắn nào được gửi đi.");
                return true;
            }
            var result = await browser.PollMessengerSetup();
            if (result is null) return true;
            if (!string.IsNullOrEmpty(result.Error)) { await browser.EndMessengerSetup(); Set("Failed", result.Error); return false; }
            var selectors = new MessengerSelectors(result.IncomingMessages, result.OutgoingMessages, result.MessageIdAttribute);
            // Reload and re-identify both exact samples before persisting anything.
            await browser.VerifyMessengerSetup(target, result);
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var temp = SettingsPath + ".new";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(selectors), ct);
            File.Move(temp, SettingsPath, true);
            options.Value.IncomingMessages = selectors.IncomingMessages;
            options.Value.OutgoingMessages = selectors.OutgoingMessages;
            options.Value.MessageIdAttribute = selectors.MessageIdAttribute;
            Set("Completed", "Đã lưu và kiểm tra nhận diện Messenger. Bot sẽ lấy mốc lịch sử, rồi báo các tin mới; không cần khởi động lại API.");
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            try { await browser.EndMessengerSetup(); } catch { }
            Set("Failed", "Chưa thiết lập được. Kiểm tra đã đăng nhập Messenger, mở được hội thoại và có tin của cả hai phía. Không thay đổi cấu hình đang dùng.");
            return false;
        }
    }
    public static void Load(string path, RentalOptions options)
    {
        if (!File.Exists(path)) return;
        try
        {
            var settings = JsonSerializer.Deserialize<MessengerSelectors>(File.ReadAllText(path));
            if (settings is null || string.IsNullOrWhiteSpace(settings.IncomingMessages) || string.IsNullOrWhiteSpace(settings.OutgoingMessages) || string.IsNullOrWhiteSpace(settings.MessageIdAttribute)) return;
            options.IncomingMessages = settings.IncomingMessages; options.OutgoingMessages = settings.OutgoingMessages; options.MessageIdAttribute = settings.MessageIdAttribute;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine("Không đọc được cấu hình nhận diện Messenger đã lưu. Có thể thiết lập lại trên trang Khách & Messenger."); }
    }
}

public record MessengerSetupResult(string IncomingMessages, string OutgoingMessages, string MessageIdAttribute, string IncomingId, string OutgoingId, string Error);

public sealed class RentalMessengerOptions(IWebHostEnvironment environment) : IPostConfigureOptions<RentalOptions>
{
    public void PostConfigure(string? name, RentalOptions options) => RentalMessengerSetup.Load(Path.Combine(environment.ContentRootPath, "App_Data", "messenger-selectors.json"), options);
}
