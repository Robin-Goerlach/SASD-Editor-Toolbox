namespace Sasd.Editor.Windows;

/// <summary>
/// Per-window editing modes. Keeping modes on the view matches the historical
/// behavior while allowing multiple views of one document to differ.
/// </summary>
public sealed class EditorWindowOptions
{
    private int _leftMargin;
    private int _rightMargin = 79;
    private int _tabSize = 4;

    public bool InsertMode { get; set; } = true;

    public bool WordWrap { get; set; }

    public bool AutoIndent { get; set; }

    public int LeftMargin
    {
        get => _leftMargin;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value > _rightMargin)
            {
                throw new ArgumentException("Left margin cannot exceed right margin.", nameof(value));
            }

            _leftMargin = value;
        }
    }

    public int RightMargin
    {
        get => _rightMargin;
        set
        {
            if (value < _leftMargin)
            {
                throw new ArgumentException("Right margin cannot be smaller than left margin.", nameof(value));
            }

            _rightMargin = value;
        }
    }

    public int TabSize
    {
        get => _tabSize;
        set => _tabSize = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public EditorWindowOptions Clone() => new()
    {
        InsertMode = InsertMode,
        WordWrap = WordWrap,
        AutoIndent = AutoIndent,
        RightMargin = RightMargin,
        LeftMargin = LeftMargin,
        TabSize = TabSize
    };
}
