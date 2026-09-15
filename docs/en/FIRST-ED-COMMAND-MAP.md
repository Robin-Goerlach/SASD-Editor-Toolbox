# FIRST-ED command map

## Purpose

This document records the first compatibility input layer for the SASD Editor Toolbox. It is based on the FIRST-ED quick command reference and the command-dispatch discussion in the Turbo Editor Toolbox 1.0 handbook.

The historical design classifies ordinary text separately from commands and delegates Ctrl-K, Ctrl-O and Ctrl-Q to prefix dispatchers. SASD preserves that behavior in `FirstEdKeyMap`, but replaces DOS scan codes with the UI-neutral `EditorKeyStroke` model.

## Architectural boundary

`FirstEdKeyMap` does **not** edit text and does **not** prompt the user. It returns one of four results:

- ordinary text to insert;
- a semantic editor command;
- a pending command prefix;
- ignored/unmapped input.

Commands that historically asked the user for a filename, number, search string or confirmation carry an `EditorCommandArgumentKind`. The host collects that value and then creates an `EditorCommandRequest`. This keeps keyboard compatibility independent from console, WPF, WinForms and web UI code.

## Primary commands

| Key | Semantic command |
|---|---|
| Ctrl-A | Word left |
| Ctrl-S | Character left |
| Ctrl-D | Character right |
| Ctrl-F | Word right |
| Ctrl-E | Line up |
| Ctrl-X | Line down |
| Ctrl-C | Page down |
| Ctrl-W | Scroll up |
| Ctrl-Z | Scroll down |
| Ctrl-P | Insert control character (host prompt) |
| Ctrl-J | Beginning/end of line toggle |
| Enter / Ctrl-N | Insert line |
| Ctrl-G / Delete | Delete right character |
| Backspace / Ctrl-H | Delete left character |
| Ctrl-R | Page up |
| Ctrl-T | Delete right word |
| Ctrl-Y | Delete line |
| Ctrl-B | Reformat paragraph |
| Ctrl-V | Toggle insert/overtype |
| Ctrl-L | Find next occurrence |
| Escape | Undo |

Arrow, Home, End and Page keys are also accepted as modern aliases. They do not replace the historical control-key bindings.

## Ctrl-K prefix

| Sequence | Semantic command |
|---|---|
| Ctrl-K B | Begin block |
| Ctrl-K K | End block |
| Ctrl-K C | Copy block |
| Ctrl-K V | Move block |
| Ctrl-K Y | Delete block |
| Ctrl-K H | Hide/show block |
| Ctrl-K R | Read file (filename prompt) |
| Ctrl-K W | Write file (filename prompt) |
| Ctrl-K S | Save file |
| Ctrl-K T | Set tab width (number prompt) |
| Ctrl-K X | Exit (confirmation) |
| Ctrl-K M | Set marker (number prompt) |
| Ctrl-K 1..9 | Set numbered marker |

## Ctrl-O prefix

| Sequence | Semantic command |
|---|---|
| Ctrl-O X | Window down / next window |
| Ctrl-O E | Window up / previous window |
| Ctrl-O G | Go to window (number prompt) |
| Ctrl-O J | Link windows (two-number prompt) |
| Ctrl-O Y | Delete window (number prompt) |
| Ctrl-O O | Create window (two-number compatibility prompt) |
| Ctrl-O W | Toggle word wrap |
| Ctrl-O C | Center line |
| Ctrl-O I | Go to column (number prompt) |
| Ctrl-O N | Go to line (number prompt) |
| Ctrl-O K | Change case |
| Ctrl-O L | Set left margin |
| Ctrl-O R | Set right margin |
| Ctrl-O S | Set undo limit |
| Ctrl-O 1..9 | Go to numbered window |

The historical create-window command asked for both a size and a source window. SASD records the two-value requirement now; actual screen-layout semantics remain a later host/rendering milestone.

## Ctrl-Q prefix

| Sequence | Semantic command |
|---|---|
| Ctrl-Q C | Bottom of file |
| Ctrl-Q R | Top of file |
| Ctrl-Q I | Toggle auto-indent |
| Ctrl-Q B | Top of block |
| Ctrl-Q K | Bottom of block |
| Ctrl-Q J | Jump to marker (number prompt) |
| Ctrl-Q A | Find/replace (find+replace prompt) |
| Ctrl-Q F | Find pattern (text prompt) |
| Ctrl-Q D | End of line |
| Ctrl-Q S | Beginning of line |
| Ctrl-Q Y | Delete to end of line |
| Ctrl-Q 1..9 | Jump to numbered marker |

## Current transfer status

This milestone implements the complete FIRST-ED **input mapping contract** and tests the prefix state machine. Some semantic commands already have command processors in `EditorCommandDispatcher`; prompt-driven file/window/search details are intentionally transferred in later, separately testable steps. This prevents UI prompting and historical keyboard conventions from leaking into the editor engine.
