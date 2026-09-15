namespace Sasd.Editor.Search;

/// <summary>
/// Options for literal forward search. FIRST-ED compatibility searches do not
/// wrap by default; modern hosts can explicitly enable wrap-around.
/// </summary>
public sealed record SearchOptions(
    bool CaseSensitive = false,
    bool WholeWord = false,
    bool WrapAround = false);
