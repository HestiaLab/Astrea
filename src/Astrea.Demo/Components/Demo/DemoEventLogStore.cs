namespace Astrea.Demo.Components.Demo;

public sealed record DemoLogEntry(int Id, string Message);

/// <summary>Shared, bounded event log for every demo on the page.</summary>
public sealed class DemoEventLogStore
{
    private const int MaxLines = 20;
    private readonly List<DemoLogEntry> _entries = new();
    private int _nextId;

    public event Action? Changed;

    public IReadOnlyList<DemoLogEntry> Entries => _entries;

    public void Add(string message)
    {
        _entries.Insert(0, new DemoLogEntry(++_nextId, message));
        if (_entries.Count > MaxLines)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Changed?.Invoke();
    }
}
