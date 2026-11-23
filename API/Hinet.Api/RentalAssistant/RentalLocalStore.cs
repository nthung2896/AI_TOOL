using System.Security.Cryptography;
using System.Text.Json;

namespace Hinet.Api;

public class RentalLocalData
{
    public int Version { get; set; } = 1;
    public RentalState State { get; set; } = new();
    public List<RentalRoom> Rooms { get; set; } = new();
    public List<RentalGroup> Groups { get; set; } = new();
    public List<RentalThread> Threads { get; set; } = new();
    public List<RentalPost> Posts { get; set; } = new();
    public List<RentalDelivery> Deliveries { get; set; } = new();
    public List<RentalAppointment> Appointments { get; set; } = new();
}

public sealed class RentalLocalStore : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object gate = new();
    private readonly FileStream processLock;
    private RentalLocalData data;
    public string DirectoryPath { get; }
    public RentalOptions Options { get; }
    public RentalLocalStore(string directory)
    {
        DirectoryPath = Path.GetFullPath(directory);
        Directory.CreateDirectory(DirectoryPath);
        // One process owns the local data and browser profile. A second launch fails before loading state.
        processLock = new FileStream(Path.Combine(DirectoryPath, "instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Options = Load<RentalOptions>("settings.json") ?? new RentalOptions
        {
            AiMode = "template", BrowserEnabled = true, Enabled = true,
            ManagementKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ProfilePath = Path.Combine(DirectoryPath, "facebook-profile")
        };
        if (Options.ManagementKey.Length < 32) Options.ManagementKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        Options.ProfilePath = Path.Combine(DirectoryPath, "facebook-profile");
        data = Load<RentalLocalData>("data.json") ?? new();
        foreach (var delivery in data.Deliveries.Where(x => x.Status == "Sending")) delivery.Status = "NeedsReview";
        foreach (var thread in data.Threads.Where(x => x.Status == "Replying")) { thread.Status = "NeedsReview"; thread.AutoReply = false; }
        data.State.Paused = true; // Always start paused; opening the app never sends anything.
        Save("data.json", data);
        Save("settings.json", Options);
    }
    private T? Load<T>(string name)
    {
        var path = Path.Combine(DirectoryPath, name);
        // Corrupt data fails visibly instead of silently replacing customer records with an empty file.
        return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : default;
    }
    private void Save<T>(string name, T value)
    {
        var path = Path.Combine(DirectoryPath, name);
        var temp = path + ".new";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, value, Json);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Copy(path, path + ".backup", true);
        File.Move(temp, path, true);
    }
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
    public RentalLocalData Read() { lock (gate) return Clone(data); }
    public T Update<T>(Func<RentalLocalData, T> update)
    {
        lock (gate)
        {
            var next = Clone(data);
            var result = update(next);
            Save("data.json", next);
            data = next;
            return Clone(result);
        }
    }
    public void Configure(Action<RentalOptions> configure)
    {
        lock (gate)
        {
            var next = Clone(Options);
            configure(next);
            Save("settings.json", next);
            foreach (var property in typeof(RentalOptions).GetProperties()) property.SetValue(Options, property.GetValue(next));
        }
    }
    public void Dispose() => processLock.Dispose();
}
