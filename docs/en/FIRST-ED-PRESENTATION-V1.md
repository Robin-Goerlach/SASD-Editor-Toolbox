# FIRST-ED physical presentation completion (2026-10-21)

## Historical requirements

The Turbo Editor Toolbox Owner's Handbook, chapter 11 (printed pp. 69-72),
describes five independently configurable color/attribute roles: ordinary text
(Txtcolor), window status (Bordcolor), selected block (Blockcolor), command line
(Cmdcolor) and user-colored text (Usercolor). The reference descriptions of
EditColorLine/EditColorFile (pp. 137-138) state that Colored takes precedence
over Blockcolor and Txtcolor. FIRST-ED can also show a right-edge indicator:
hard line end `<`, file-exhausted display row `.`, or blank for a soft Wrapped
line end. MicroStar is documented as leaving these indicators off.

## Implementation

The editor core *already* supplied InBlock, Wrapped and UserColored line flags.
The new `ConsoleFirstEdPresentationPolicy` maps them to host display roles and
the optional right-edge marker. The console renderer now consumes those roles.
It reserves a marker cell only when the indicator option is enabled; the
terminal's last physical column is still excluded from writes to prevent
implicit auto-wrap. Horizontal visibility and physical caret positioning use
the reduced text width, while document positions remain unchanged.

The particular ConsoleColor values are a **modern host theme choice**. The
manual defines the five roles but not a portable RGB/ANSI color palette.
Other hosts are free to choose different schemes. Physical Console APIs and
the option `ShowRightEdgeMarkers` remain in the sample project, not Core.

The existing safe `ConsoleFirstEdRowProjection` continues to sanitize control
characters before any Console.Write. Flags/markers are purely visual: they
must not alter StoredText, File I/O, Dirty, cursor state or Undo.

## Acceptance evidence and limitations

`FirstEdPresentationPolicyTests` checks role precedence, hard/soft/EOF
markers, disabled indicators, linked-view flags and inert terminal projection.
These tests are headless. Real Windows and Linux terminal checks (including
narrow windows, themes, resizing and cursor placement) remain a manual V1 gate.
This is **not** a clone of DOS video memory, the CGA retrace path, or MicroStar
menus/pop-ups/background print features.
