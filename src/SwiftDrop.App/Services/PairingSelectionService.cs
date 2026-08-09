using SwiftDrop.Core.Models;

namespace SwiftDrop.App.Services;

public sealed class PairingSelectionService
{
    private readonly object _gate = new();
    private PairingPayload? _current;

    public event EventHandler? Changed;

    public PairingPayload? Current
    {
        get
        {
            lock (_gate) return _current;
        }
    }

    public void Set(PairingPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        lock (_gate) _current = payload;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        lock (_gate) _current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
