namespace Astrea.Demo.Components.Demo;

public sealed record ResourceTableDemoStatus(
    int Rows,
    string SelectedRow,
    int UndoCount,
    int RedoCount,
    bool Resizable);
