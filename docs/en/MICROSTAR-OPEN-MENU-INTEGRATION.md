# MicroStar F10 file switching

The F10 menu now includes **P Save/Open** and **A Abandon/Open**.
The read-only file browser selects the replacement path; no document
is modified by browsing.

Save/Open uses the existing Save File prompt and semantic dispatcher.
A missing path, denied overwrite or failed save aborts the switch.
Abandon/Open requires typing the exact word `DISCARD` when the
source has unsaved changes. The menu choice alone never implies consent.

The host rechecks the initiating window, source version and dirty state.
Core then stages and validates all incoming lines independently before
reattaching only that view. Other linked views keep the original document,
and source Undo/marker cleanup follows existing session lifecycle rules.

A successfully saved source remains saved if the subsequent destination
load fails. The letter shortcuts are SASD host choices, not a claim of
historical scan-code identity. Interactive terminal acceptance is pending.
