# MicroStar Save & Open / Abandon & Open — preflight policy

This milestone establishes the **non-destructive preflight contract** for
the two historically described file-change workflows, independently from
their eventual terminal integration.

`MicroStarOpenPolicy.Plan` distinguishes Cancel, SaveThenOpen and
AbandonThenOpen. SaveThenOpen always requires a completed successful save
before any transition. It never silently falls back to discarding edits.
AbandonThenOpen requires a **separate explicit confirmation** when the
source is dirty; choosing a menu item is not itself permission to lose data.

`CanCommit` validates that the document version has not changed while
asynchronous work was in progress. A successful save must also leave the
original document clean. The policy contains no prompts, Console access,
filesystem writes, or editor mutations; every choice is unit-testable.

**Important:** this is a first architectural slice, **not a working
Save & Open or Abandon & Open menu action yet**. The next integration step
must decode the destination into an independent staged document, check the
initiating view's identity and version, then reattach the view atomically
via the session's lifecycle API. Opening must never destroy source content
before successful decoding. Related linked-view and Undo invariants need
explicit end-to-end tests.
