namespace Sasd.Editor.Search;

public sealed record SearchOptions(
    bool CaseSensitive = false,
    bool WholeWord = false,
    bool WrapAround = true);
