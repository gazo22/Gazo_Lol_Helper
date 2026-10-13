using RiftLens.Infrastructure.Riot;

namespace RiftLens.Infrastructure.Tests.Support;

internal sealed class TestKeyStore(string? key) : IRiotKeyStore
{
    private RiotKeyState? _state;

    public string? Key { get; set; } = key;

    public RiotKeyState State => Key is null ? RiotKeyState.Missing : _state ?? RiotKeyState.Unknown;

    public event Action? KeyChanged;

    public string? GetKey() => Key;

    public Task SetKeyAsync(string key, CancellationToken ct = default)
    {
        Key = key;
        KeyChanged?.Invoke();
        return Task.CompletedTask;
    }

    public void ReportAccepted() => _state = RiotKeyState.Valid;

    public void ReportRejected() => _state = RiotKeyState.Rejected;
}
