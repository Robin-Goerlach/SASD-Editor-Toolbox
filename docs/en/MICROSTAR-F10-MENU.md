# MicroStar F10 menu — executable first application slice

The 1985 handbook describes a **MicroStar application pull-down menu on F10**
(see printed pp. 42-53). The FIRST-ED editor kernel and the MicroStar sample
application must remain separate.

This implementation adds a working **F10 menu entry point** to the .NET
terminal sample:

- The host intercepts normalized, unmodified F10 before the FIRST-ED key map;
- an existing Ctrl-K/O/Q prefix is reset;
- menu selection returns one existing `EditorCommandBinding`;
- the existing host prompt resolver and semantic dispatcher execute it;
- cancellation or an invalid selection changes no editor text/state;
- headless tests cover every binding and cancelled/unsupported inputs.

The first presentation uses the existing **safe command-line prompt**, not a
true overlapping screen popup or a full pointer-operated pull-down menu.
This is an intentionally limited but usable terminal MVP: full
`MakeWindow`/`RestoreWindow` popups, richer menu navigation, directory
management, block file commands, background printing and Lightning spell
integration remain open.

**Source boundary:** F10's role is historically documented; the SASD
selection letters and item order are modern UI decisions, not a claim of
an exact Borland MicroStar keyboard mapping. All editing logic remains
inside the reusable editor core.
