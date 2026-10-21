# V1 terminal-output safety and smoke acceptance

**Scope:** FIRST-ED .NET sample host; October 2026. This document specifies
repeatable observations and remaining *manual* release gates. It is **not** a
claim that interactive Windows/Linux testing has already been performed.

## Why this is a release-relevant issue

The editor intentionally accepts and stores literal ASCII control characters
via `EditInsertCtrlChar` (Ctrl-P). Its previous terminal renderer wrote
stored document text **directly** through `Console.Write`, with only a
`string.Length`-based clip. Consequently a document containing ASCII ESC
followed by `[2J` could invoke a terminal clear-screen sequence, while
carriage return, newline, tab or backspace could move the terminal cursor or
disrupt adjacent stacked-window/status rows. The same unsafe output path
previously handled filenames, status text and messages.

The host now uses one pure `ConsoleFirstEdRowProjection` boundary before
**every** terminal row write. ASCII C0 controls are shown in caret notation:
`ESC` as `^[`, `CR` as `^M`, `LF` as `^J`, and `NUL` as `^@`.
`DEL` is `^?`; C1 controls including `0x9B` (8-bit CSI) use inert ASCII
hex markers such as `[9B]`. Invalid UTF-16 surrogates are replaced with a
visible `?`. **The document and save bytes are never modified by rendering.**

Clipping works on complete Unicode *text elements* rather than slicing UTF-16
in the middle of a surrogate pair or combining sequence. A host-only
conservative cell-width estimate handles common CJK characters and emoji as
two cells, ordinary base/combining sequences as one. Cursor projection accounts
for visible control notation and these estimated cell widths, including
virtual columns. Rendering obtains the **full stored logical line**, not the
platform-neutral viewport's pre-truncated UTF-16 string.

This does not claim exact `wcwidth` compatibility, ambiguous-width glyph
parity, every emoji ZWJ sequence's width, or automatic right-to-left reordering.
Those behaviors depend on the actual terminal and font; smoke tests remain
mandatory. Core's logical UTF-16 `Cursor.Column`, `LeftColumn` and
portable `EditorViewportBuilder` contracts were not changed.

## Automated evidence

`ConsoleFirstEdRowProjectionTests` exercises:

- C0, DEL, 8-bit CSI/C1 and ESC escape sequences producing **inert ASCII**
  instead of terminal commands;
- actual semantic ESC insertion followed by projection and Undo, proving the
  stored control character remains unchanged;
- narrow clipping that never emits half of `^[`, a surrogate pair or a
  combined glyph;
- common CJK/emoji width, combining-accent width and physical cursor offset
  after expanding a stored control character;
- horizontal offsets landing *inside* a Unicode grapheme, virtual-space
  cursor columns, malformed surrogate input and invalid geometry.

These are deterministic headless unit/integration tests, not a substitute for
a real terminal or operating-system-specific keyboard events.

## Manual Windows and Linux smoke checklist (not yet signed off)

Record operating system/version, terminal emulator, font, .NET runtime,
terminal dimensions, input method, screenshot/observation and pass/fail for
each item. Test Windows Terminal/PowerShell on Windows 11 and a Linux PTY
(e.g. GNOME Terminal or xterm on Debian/Ubuntu).

1. **Raw ESC remains text:** With Ctrl-P then `[`, enter the printable
   characters `[2J`. Confirm the line displays `^[[2J`; the terminal must
   **not** clear its screen and status rows must stay intact. Undo must
   restore the document.
2. **Other controls:** Ctrl-P then `M` (`CR`), `J` (`LF`) and `I`
   (`TAB`) in separate trials. Expect `^M`, `^J`, `^I` inline,
   with no physical cursor jump to another row.
3. **Narrow and resized terminal:** Resize while a control-code line is
   visible, including one- and two-cell content areas if the terminal permits.
   No wrap into the neighboring stacked status row; do not assume a window
   layout requiring more rows will fit.
4. **Unicode:** Enter or load `a界b`, `a😀b`, `é` and a
   joined emoji sequence. Check clipping at right edge, physical caret
   placement, and whether the terminal's glyph widths match estimates.
   Record any ambiguous-width/ZWJ deviations rather than silently accepting
   them.
5. **Prompt input:** Verify raw Ctrl-U and Escape cancel file/find/replace
   prompts. Confirm AltGr printable keys are not mistaken for Ctrl-U.
   With a long filename and combining/supplementary Unicode, verify complete
   answer retention and Backspace removal of a whole text element.
6. **Linked windows:** Split into several stacked windows; show a line
   containing an escaped control in one and navigate the other. Confirm
   neither status nor physical cursor is corrupted.
7. **Persistence:** Save and reload a document containing a literal control
   code using the appropriate file codec. Rendering must remain escaped;
   the stored bytes must retain the original control value.

For each failed check, log reproduction steps and terminal specifics. Do not
claim V1.0 terminal acceptance merely because GitHub CI passes.

## Architecture and future work

The projection is **sample-host code only**. It does not change Unicode file
storage, Core commands, historical line metadata, Undo, or other UI backends.
The implementation is intentionally reviewable, not optimized for rendering
throughput. Should exact cross-terminal column layout become a V1 requirement,
it should be addressed via an explicit host capability / tested cell-width
policy, **not** by redefining the editor document's character offsets.
