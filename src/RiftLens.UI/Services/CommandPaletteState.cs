namespace RiftLens.UI.Services;

/// <summary>État partagé de la palette de commandes (Ctrl+K).</summary>
public sealed class CommandPaletteState
{
    public bool IsOpen { get; private set; }

    public event Action? Changed;

    public void Open() => Set(true);

    public void Close() => Set(false);

    public void Toggle() => Set(!IsOpen);

    private void Set(bool open)
    {
        if (IsOpen == open)
        {
            return;
        }

        IsOpen = open;
        Changed?.Invoke();
    }
}
