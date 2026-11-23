namespace Hinet.Api;
public record WorkerActivity(bool Running, string Task, DateTimeOffset Since);
public sealed class RentalWorkerStatus
{
    private readonly object gate = new();
    private bool running;
    private string task = "idle";
    private DateTimeOffset since = DateTimeOffset.UtcNow;
    public WorkerActivity Snapshot() { lock (gate) return new(running, task, since); }
    public IDisposable Start()
    {
        lock (gate) { running = true; since = DateTimeOffset.UtcNow; }
        return new EndScope(() => { lock (gate) { running = false; task = "idle"; since = DateTimeOffset.UtcNow; } });
    }
    public async Task Run(string name, Func<Task> work)
    {
        using var scope = Begin(name);
        await work();
    }
    public async Task<T> Run<T>(string name, Func<Task<T>> work)
    {
        using var scope = Begin(name);
        return await work();
    }
    private IDisposable Begin(string name)
    {
        lock (gate) { task = name; since = DateTimeOffset.UtcNow; }
        return new EndScope(() => { lock (gate) { task = "idle"; since = DateTimeOffset.UtcNow; } });
    }
    private sealed class EndScope(Action end) : IDisposable { public void Dispose() => end(); }
}
