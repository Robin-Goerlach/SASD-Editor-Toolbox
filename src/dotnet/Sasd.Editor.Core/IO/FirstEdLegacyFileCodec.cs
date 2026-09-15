using System.Text;
using Sasd.Editor.Model;

namespace Sasd.Editor.IO;

/// <summary>
/// Compatibility codec for the Turbo Editor Toolbox text-file convention.
///
/// The historical writer used a carriage return with its high bit set
/// (0x8D / decimal 141) to mark a logical line that resulted from word wrap.
/// Ordinary line boundaries are emitted as DOS CR/LF. The byte-oriented codec
/// uses the Latin-1 character range deliberately: this is a compatibility
/// format, not the modern UTF-8 persistence format supplied by FileTextStorage.
/// </summary>
public sealed class FirstEdLegacyFileCodec : IEditorFileCodec
{
    public const byte WrappedLineTerminator = 0x8D;

    public async ValueTask<IReadOnlyList<EditorLineSnapshot>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = await File.ReadAllBytesAsync(Path.GetFullPath(path), cancellationToken).ConfigureAwait(false);
        return Decode(bytes);
    }

    public async ValueTask WriteAsync(
        string path,
        IReadOnlyList<EditorLineSnapshot> lines,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(lines);

        var bytes = Encode(lines);
        await File.WriteAllBytesAsync(Path.GetFullPath(path), bytes, cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<EditorLineSnapshot> Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return Array.Empty<EditorLineSnapshot>();
        }

        var lines = new List<EditorLineSnapshot>();
        var current = new List<char>();
        var endedWithTerminator = false;

        for (var index = 0; index < bytes.Length; index++)
        {
            var value = bytes[index];
            var wrapped = value == WrappedLineTerminator;
            var ordinaryCarriageReturn = value == (byte)'\r';
            var ordinaryLineFeed = value == (byte)'\n';

            if (wrapped || ordinaryCarriageReturn || ordinaryLineFeed)
            {
                lines.Add(new EditorLineSnapshot(
                    new string(current.ToArray()),
                    wrapped ? EditorLineFlags.Wrapped : EditorLineFlags.None));
                current.Clear();
                endedWithTerminator = true;

                // Accept CR/LF and, defensively, 0x8D/LF as one boundary.
                if ((wrapped || ordinaryCarriageReturn)
                    && index + 1 < bytes.Length
                    && bytes[index + 1] == (byte)'\n')
                {
                    index++;
                }

                continue;
            }

            current.Add((char)value);
            endedWithTerminator = false;
        }

        if (current.Count > 0 || !endedWithTerminator)
        {
            lines.Add(new EditorLineSnapshot(new string(current.ToArray())));
        }

        return lines;
    }

    private static byte[] Encode(IReadOnlyList<EditorLineSnapshot> lines)
    {
        if (lines.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = new List<byte>();

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            foreach (var character in line.Text)
            {
                if (character > byte.MaxValue)
                {
                    throw new EncoderFallbackException(
                        "The FIRST-ED compatibility file format can only encode 8-bit Latin-1 characters.");
                }

                bytes.Add((byte)character);
            }

            if (lineIndex == lines.Count - 1)
            {
                // A final wrapped marker is retained if a caller deliberately
                // supplies one. Normally a wrapped paragraph has a following
                // logical continuation line.
                if (line.Flags.HasFlag(EditorLineFlags.Wrapped))
                {
                    bytes.Add(WrappedLineTerminator);
                }

                continue;
            }

            if (line.Flags.HasFlag(EditorLineFlags.Wrapped))
            {
                bytes.Add(WrappedLineTerminator);
            }
            else
            {
                bytes.Add((byte)'\r');
                bytes.Add((byte)'\n');
            }
        }

        return bytes.ToArray();
    }
}
