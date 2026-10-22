# FIRST-ED command-line helpers — historical V1 acceptance audit

**Status:** Focused host-model review, 21 October 2026. **Not a full historical
terminal, keyboard or typeahead acceptance.** Original sources: Borland *Turbo
Editor Toolbox Owner's Handbook* (1985), printed pages 120–122 and 237.

The historical command line uses DOS screen cells, a global `Cmdcol` and a
Pascal `Varstring`. In SASD the terminal host owns the command row, while the
reusable Core owns semantic commands and a separately bounded typeahead
abstraction. This separation is intentional: copying the DOS memory/column
representation into Core would make later WinForms/WPF/web hosts dependent on
terminal implementation details.

| Historical entry | Handbook behavior | SASD reference host | Acceptance boundary |
| --- | --- | --- | --- |
| `EditAppchar` (p. 120) | Appends one character to a Varstring; length must remain below `Defnocols` or the old routine overflows. | `ConsolePromptEditor` keeps an expandable managed `StringBuilder`; the complete response remains available to the semantic resolver. | **Modern replacement.** No fixed Pascal buffer or `Defnocols` promise. A terminal-width response is not truncated. |
| `EditAppcmdnam` (p. 121) | Appends command-line label and advances global `Cmdcol`; an oversized label can overwrite the upper status row. | `ConsoleFirstEdRenderer.ReadPrompt` always paints the dedicated final row and positions the caret inside its width. | **Modern safety divergence:** no global `Cmdcol` and no intentional overwriting of window status. |
| `EditAskfor` (p. 122) | Reads until Return; zero-length Return is valid. `Interactive=false` draws from typeahead; true reads the keyboard directly. Control codes display as caret notation; input stops when the old command row fills. Ctrl-U can recover the previous Varstring contents. | `ConsolePromptEditor` distinguishes Enter (submit even empty) from Escape/Ctrl-U (cancel). Non-printable controls are not inserted into filename/search prompts. Long answers are preserved while the visible projection right-scrolls. The interactive host uses direct `Console.ReadKey`; shared Core has a separate typeahead buffer. | **Partial host equivalence, intentional differences:** Ctrl-U is an abort instead of Varstring recall; no caret notation for control entries, width-based input rejection, or promised integration of interactive prompts with the Core typeahead queue. |
| `EditZapcmdnam` (p. 237) | Blanks the command line and resets `Cmdcol` to the first column. | Each `ReadPrompt` repaints the dedicated row from a fresh prompt model; the next command does not inherit a previous global cursor offset. | **Host-level equivalent responsibility**, not Pascal display-memory parity. |

## Concrete changes in this audit

1. **Terminal cancellation across platforms.** `ConsolePromptKeyPolicy` now
   recognizes Escape and Ctrl-U both by named key/chord and by the raw ASCII
   bytes `0x1B`/`0x15`. Some Unix pseudo-terminals return
   `ConsoleKey.NoName` for the latter. Both `ConsolePromptEditor` and the
   Ctrl-P single-character policy share the same normalization. AltGr
   printable input continues to work. This is a **real corrected host gap**,
   not a new semantic command.
2. **Unicode-safe local editing.** Prompt Backspace now removes one complete
   Unicode text element, including a supplementary-plane surrogate pair or
   a base letter plus combining accent. Right-scrolled projections align
   their starting index to a text-element boundary rather than displaying
   half a surrogate or accent sequence. The **complete answer is unchanged**
   by projection. These are host-only correctness improvements; exact visual
   cell widths for wide/emoji glyphs depend on the terminal and remain a
   manual smoke-test responsibility.
3. **Pure regression evidence.** The existing prompt tests now cover raw
   unnamed Ctrl-U/Escape, AltGr preservation, Unicode element deletion,
   narrow Unicode projections and unchanged full answers. The Ctrl-P tests
   also verify the shared raw-byte cancellation rule. Existing tests retain
   null-cancel versus explicitly empty Enter, narrow ASCII projection,
   filename/replace cancellation and Ctrl-P ASCII translation.

## What remains before V1.0 sign-off

- Do **not** mark the four entries as directly verified portable historical
  routines simply because host-only tests exist. The portable contract has
  different boundaries; the historical inventory intentionally classifies
  these as `host-sample`.
- Run actual Windows Console/Terminal and Linux PTY smoke tests for Ctrl-U,
  Escape, AltGr, narrow windows, combined Unicode and terminal redraw.
- Evaluate whether a distinct **opt-in classic Askfor mode** with prior
  Varstring recall and command-row length limits is worth implementing as a
  separate host policy. It must not silently break the current cancel-on-
  Ctrl-U safety contract used by Save/Replace prompts.
- Keep video-memory/BIOS conventions and global `Cmdcol` out of Core.

## Reusable prompt reset (modern host feature)

The headless `ConsolePromptEditor.Reset()` now explicitly clears its
complete answer buffer. It has no console side effects: painting the command
row is still the host renderer's responsibility. Tests demonstrate that an
earlier filename cannot leak into a later prompt and that a prior Escape
cancellation does not poison the next input attempt.

This contributes to predictable host lifecycle handling. It is **not** a
claim of exact `EditZapcmdnam` or `EditAskfor` historical behavior:
DOS `Cmdcol`, Varstring recall, and physical terminal interaction still
require the separate documented review.

## Delete versus Backspace in append-only prompts

The portable `ConsolePromptEditor` keeps its input caret at the end.
Consequently, **Delete** has no following character to erase, while
**Backspace** removes the preceding user-visible text element. This
difference is now enforced explicitly rather than depending on a terminal's
choice of `KeyChar` for Delete. A deterministic headless test covers
both keys on the same input.

This is a modern host-input policy; historical Pascal Varstring edit keys,
`Cmdcol` handling and real terminal integration still require independent
acceptance and are not presented as Borland parity.

## Raw terminal Return without a named key

On some POSIX terminals Return reaches the host as a raw CR (`0x0D`) or
LF (`0x0A`) with `ConsoleKey.NoName`. `ConsolePromptEditor.Process` now
accepts both as submission, exactly like a named Enter key, without
appending the control character to a search term or filename. This check
comes after the host cancellation policy so cancellation remains dominant.
A headless parameterized regression test covers both raw representations.
Real terminal smoke testing and historical DOS `EditAskfor` parity remain
separate acceptance tasks.
