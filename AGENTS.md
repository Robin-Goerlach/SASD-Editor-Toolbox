# AGENTS.md - SASD Editor Toolbox

This file is the repository-level working contract for Codex and other coding agents.

The goal is to let an agent make substantial progress with as little human micromanagement as possible while preserving the project's architecture, clean-room/legal boundary, test discipline, bilingual documentation and Git history.

## 1. Repository identity

This repository is **SASD-Editor-Toolbox**.

Do not confuse it with **SASD-Editor-Toolkit** or import plans, source files, milestones or architectural decisions from that repository unless the user explicitly asks for cross-project work.

The Editor Toolbox is a reusable, UI-agnostic editor foundation. C#/.NET is the first active reference implementation, but it is not the normative specification. C++, Java and later implementations are peer lanes. Portable behavior lives in `spec/`, reusable cross-language examples live in `conformance/`, and language-specific source/tests/samples live below matching `src/<language>`, `tests/<language>` and `samples/<language>` roots.

## 2. Mission and current V1 target

The immediate product goal is a clean-room modern implementation of the observable functional scope of Borland's historical **Turbo Editor Toolbox 1.0 (1985)**.

The historical handbook is a **behavioral requirements/reference source only**.

Preserve observable behavior where it is known, but implement it with modern, reviewable architecture idiomatic to the target language. Historical Pascal pointer layouts, free lists, DOS scan codes, video memory, overlay mechanics and other implementation details are not compatibility requirements unless they materially affect observable behavior.

The project deliberately favors:

1. correctness;
2. clear architecture and invariants;
3. deterministic tests;
4. useful comments and documentation;
5. portability of behavior;
6. performance optimization later, behind stable boundaries and after profiling.

Do **not** over-optimize the current implementation.

## 3. Read before changing code

At the beginning of a new task, inspect the current repository instead of relying on stale assumptions.

Read in this order:

1. `README.md` - current feature status and design rules.
2. `spec/README.md` and `spec/editor-v1.md` - portable contract rules and V1 behavior.
3. `conformance/README.md` plus relevant shared cases - implementation-neutral executable examples.
4. `docs/en/BORLAND-V1-COMPATIBILITY.md` - current compatibility matrix and remaining V1 gaps.
5. `docs/en/ARCHITECTURE.md` and `docs/en/MULTI-LANGUAGE-DEVELOPMENT.md` - architecture and cross-language dependency boundaries.
6. The English document for the subsystem being changed under `docs/en/`.
7. Its German counterpart under `docs/de/`.
8. The closest production code and regression tests in the target implementation lane.
9. Recent commits touching the same subsystem when the intent is not obvious.

For historical semantics, use the handbook/manual material only as a behavioral source. If the manual is available outside this repository, do not copy it into the repository.

## 4. Source-of-truth order

When instructions or documents appear to conflict, use this order:

1. The user's current task/instruction.
2. This `AGENTS.md`.
3. `spec/editor-v1.md` for portable observable behavior.
4. Shared cases under `conformance/` when they cover the behavior in question.
5. Explicit compatibility decisions in `docs/en/BORLAND-V1-COMPATIBILITY.md` and subsystem documentation.
6. Current executable regression tests in the affected language implementation.
7. `README.md`.
8. Older prose/comments.

If a historical behavior is not actually established by the handbook/reference, do not invent certainty. Choose a conservative modern policy where necessary and document it explicitly as **modern policy**, **modernization boundary**, or **documented divergence**.

## 5. Clean-room and legal boundary

This rule is mandatory.

Allowed:

- derive behavioral requirements from the historical handbook;
- describe those requirements in your own words;
- write independent modern code in the target implementation language;
- use historical routine names briefly in compatibility documentation for traceability;
- add regression tests for observed behavior.

Not allowed:

- copy or transliterate historical Pascal source code;
- perform a 1:1 source translation from historical code;
- paste long handbook passages into comments or documentation;
- commit scanned manuals, historical source archives, logos, cover art or proprietary assets;
- claim official Borland/Embarcadero affiliation.

When source-like historical material is encountered, extract the behavior and then implement independently.

## 6. Architecture invariants

Preserve these unless the user explicitly requests an architectural change and the portable spec/docs are updated accordingly.

### 6.1 Core remains UI-agnostic

Every reusable implementation under `src/<language>` must remain independent of concrete terminal, desktop, web or mobile UI frameworks. Host-specific APIs belong in `samples/<language>` or explicitly host-facing packages, not in the portable editor core.

For .NET this continues to prohibit Console APIs, terminal libraries, WinForms, WPF, ASP.NET/browser APIs and host-specific prompts inside `Sasd.Editor.Core`. Equivalent boundaries apply in C++, Java and later ports.

### 6.2 Document and view/window are separate

Text belongs to `EditorDocument`.

`EditorWindow` is a view over a document and owns view state such as cursor, viewport and editing options. Multiple windows may share one document.

Do not collapse document identity and window identity.

### 6.3 Compatibility behavior stays isolated

Modern reusable editing behavior belongs in general services such as `EditorEngine`.

Historical FIRST-ED rules that are unusual by modern editor standards belong in explicit compatibility layers such as:

- `FirstEdCompatibilityProcessor`;
- `FirstEdPrimitiveCompatibilityProcessor`;
- `FirstEdReformatCompatibilityProcessor`;
- `FirstEdBlockCompatibilityProcessor`;
- `FirstEdKeyMap`;
- `FirstEdLegacyFileCodec`.

Do not contaminate the general editor engine with a historical quirk merely because one compatibility command needs it.

### 6.4 Structural line changes go through topology realignment

`EditorLineTopology` is the coordination point for logical line insertion/deletion effects on:

- linked-window cursor lines;
- window top-line anchors;
- markers;
- block boundaries.

When a new operation inserts or removes logical lines, audit topology explicitly. Do not scatter ad-hoc line-number repair across unrelated commands.

### 6.5 Wrapped is an outgoing soft boundary

`EditorLineFlags.Wrapped` means the boundary **after** that logical line is a soft word-wrap continuation.

It is paragraph/file-format structure, not merely a rendering hint.

Keep automatic wrapping, reformatting, New Line behavior and the legacy file codec consistent with this rule.

### 6.6 Virtual columns are valid state

A non-negative cursor column may be greater than the physical length of the current line.

Do not silently clamp a valid virtual column during unrelated structural normalization.

### 6.7 Host-neutral window geometry

Physical stacked-window allocation belongs in `EditorWindowLayout` / `EditorWindowFrame`, not in a console renderer.

Historical Create/Delete Window geometry and the three-row minimum are compatibility behavior. Dynamic modern terminal resizing is host policy and must remain documented separately.

### 6.8 Input layers stay separated

The intended path is conceptually:

```text
host input
  -> normalized EditorKeyStroke
  -> EditorTypeaheadBuffer
  -> FirstEdKeyMap
  -> semantic EditorCommandRequest
  -> EditorCommandDispatcher
  -> editor services
```

Prompt metadata may be declared by bindings, but key mapping must not display prompts.

Macro/typeahead behavior must not depend on DOS scan-code representation.

### 6.9 Persistence layers stay separate

Modern document persistence uses `ITextStorage` / `FileTextStorage`.

Historical byte-level compatibility belongs behind `IEditorFileCodec` / `FirstEdLegacyFileCodec`.

Do not make the legacy high-bit-CR format the general storage format.

### 6.10 Scheduling remains cooperative

`EditorScheduler` / `EditorSystemLoop` preserve the input-first cooperative model.

Background work must be bounded/cooperative. Do not introduce hidden long-running work on the editor thread without cancellation/abort analysis.

### 6.11 Cross-language contract and implementation lanes

The repository uses a shared-contract / independent-implementation model:

- `spec/` is normative for portable observable behavior.
- `conformance/` contains language-neutral examples; language runners translate those cases into native APIs.
- `implementations.json` lists implementation lanes and maturity.
- `src/<language>`, `tests/<language>` and `samples/<language>` for one language form a self-contained lane.
- One language implementation must never become a runtime or build dependency of another.
- Public API names, ownership models, collection types, exception/result idioms and build systems may be language-native.
- A semantic change should update the shared contract first and add/update conformance coverage before ports intentionally diverge.
- If an implementation cannot yet support a shared case, record the gap explicitly; do not silently change the shared case to match one implementation.

The .NET code may be consulted as an executable reference, but ports should implement concepts and behavior rather than mechanically transliterating C# classes line by line.

## 7. Current V1 work priority

When the user says only “continue”, “keep going” or asks Codex to work autonomously, take the first unfinished item below unless current repository evidence shows it is already complete.

### P0 - V1 release-gate work

The independent historical block-endpoint lifecycle, semantic command-boundary validation, typed host-neutral error/resource model, abort/interruptibility audit and compound undo/navigation-state audit are implemented. `EditorUndoManager` now records a logical command as one transaction even when it spans multiple documents, and restores command-induced view/anchor state without blindly overwriting later explicit navigation. The next unfinished release-gate item is the final handbook procedure/function-index audit.

1. **Final handbook procedure/function-index audit**
   - Compare the historical procedure/function inventory against the compatibility matrix and implementation.
   - Classify each item as Implemented, Replaced by modern invariant, Not Applicable, Sample/Host concern, or intentionally deferred.
   - Add regression coverage for any small omissions found.
### P1 - Sample breadth after the reusable V1 gate

Only after the reusable V1 release gate is in good shape, or when explicitly requested:

- MicroStar-style pull-down menu sample;
- pop-up helpers;
- cooperative/background printing sample;
- other host demonstrations.

These are samples/hosts. They must not become dependencies of the reusable core.

### Explicitly defer unless requested

Do not autonomously start:

- Rope/Piece Table rewrites;
- lock-free queues;
- SIMD search;
- native backends;
- GUI frameworks;
- plugin systems;
- arbitrary script execution;
- full implementation work for additional language ports unless the user explicitly requests that language; repository scaffolding and shared-contract maintenance are allowed;
- package publication/release automation.

The repository already has stable seams for later optimization and ports. Finish behavior first.

## 8. Historical-behavior transfer workflow

For every historical routine or behavior being transferred into any implementation lane:

1. Identify the observable behavior from the handbook/reference.
2. Write the behavior down in your own words.
3. Inspect the shared contract and existing abstractions in the target implementation before adding a new one.
4. Identify architecture invariants affected: document/view, topology, block, marker, undo, wrapped boundaries, virtual columns, input, storage, scheduler.
5. Add or update regression tests for the behavior and important edge cases.
6. Implement independently in idiomatic, readable target-language code.
7. Prefer a small correctness-first implementation over clever optimization.
8. Add comments where the historical behavior or modern replacement is non-obvious.
9. Update `spec/editor-v1.md` when portable behavior changes.
10. Update the relevant English and German subsystem documentation in parallel.
11. Update the V1 compatibility matrix status/notes.
12. Run the full build/test gate.
13. Commit one coherent green checkpoint.

Never implement a routine merely because its Pascal name exists. First decide whether its responsibility is:

- observable behavior that must be implemented;
- an implementation mechanism replaced by modern invariants;
- obsolete platform machinery;
- host/sample functionality rather than core behavior.

## 9. Coding style

- Code, APIs, namespaces/packages and public identifiers are English.
- Follow idiomatic safety features in each language. In .NET, nullable reference types remain enabled.
- Public APIs should have useful documentation comments appropriate to the language where their contract is not self-evident; .NET uses XML documentation comments.
- Comments explain **why**, invariants, compatibility boundaries and trade-offs.
- Avoid comments that merely restate syntax.
- Keep methods and types focused on editor concepts.
- Prefer explicit, testable services over hidden/global mutable state.
- Avoid public APIs that expose accidental implementation details of any one backend.
- Preserve language-neutral concepts while allowing each implementation's API to remain idiomatic.
- Do not force C# naming, inheritance or memory-management patterns into C++ or Java merely for visual similarity.

Optimization policy:

- no speculative micro-optimization;
- no major buffer rewrite without profiling/evidence;
- optimize behind stable abstractions;
- protect behavior with tests before optimizing.

## 10. Tests

Every behavioral change requires regression coverage.

Tests must be:

- deterministic;
- headless;
- independent of network access;
- focused on observable behavior;
- explicit about historical behavior versus documented modern policy.

Pay particular attention to:

- empty and one-line documents;
- beginning/end of stream;
- virtual columns;
- trailing blanks;
- insert versus overtype;
- Wrapped boundaries;
- linked windows over one document;
- markers and block boundaries during structural edits;
- dirty-state changes;
- undo behavior;
- cross-document operations;
- destructive operations that intentionally cannot be undone;
- cancellation/abort safety;
- Unicode behavior where the modern implementation intentionally extends ASCII-era semantics.

Do not weaken a test simply to make CI green. Establish the intended behavior first.

## 11. Build and validation

Always validate the repository-wide language layout and shared conformance metadata:

```bash
python3 tools/verify_repository_contract.py
```

Then run the native build/test gate for every implementation lane changed by the work.

For the current .NET reference implementation the canonical solution is `Sasd.Editor.Toolbox.slnx` and the target framework is **.NET 10**:

```bash
dotnet restore Sasd.Editor.Toolbox.slnx

dotnet build Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-restore

dotnet test Sasd.Editor.Toolbox.slnx \
  --configuration Release \
  --no-build

git diff --check
git status --short
```

C++ and Java will own their build systems inside their implementation lanes once development starts; they must not be wired through the .NET project graph. GitHub Actions should keep repository-contract validation separate from language-specific build workflows so one language can evolve without introducing runtime coupling to another.

The interactive FIRST-ED terminal sample is valuable for manual smoke testing, but automated regression tests remain the acceptance gate. If CI fails because of the current change, diagnose and fix it before declaring success.

## 12. Documentation policy

Documentation is part of the implementation.

English documentation under `docs/en/` is the primary technical narrative.

Maintain the corresponding German document under `docs/de/` in parallel for meaningful compatibility/architecture changes.

Use `spec/editor-v1.md` for language-neutral observable contracts and `conformance/` for portable behavior examples that should be reusable by more than one implementation.

Use the compatibility matrix to distinguish:

- implemented historical behavior;
- implementation foundation;
- planned audit;
- documented modern policy/divergence;
- intentionally replaced platform mechanics;
- not-applicable historical mechanisms.

Do not present an inference as historically documented behavior.

## 13. Git operating mode

This is currently a single-developer repository. Unless the user instructs otherwise, Codex may work directly on `main`.

At the start:

```bash
git status --short --branch
git pull --ff-only
git log -5 --oneline
```

Rules:

- do not create a branch merely out of habit;
- do not force-push;
- do not rewrite published history;
- do not amend already-pushed commits;
- do not use destructive reset to discard unknown user work;
- never mix unrelated local user changes into an agent commit;
- use focused commit messages;
- keep the worktree clean at a completed checkpoint.

After a substantial commit, push when credentials/network are available and check the corresponding GitHub Actions run.

## 14. Autonomous authority

Inside the boundaries above, Codex should make routine engineering decisions without asking for permission.

Codex may autonomously:

- inspect the repository and history;
- implement the next well-defined V1 item;
- add/refactor internal types;
- add tests;
- update specs and bilingual docs;
- correct documentation drift;
- fix failures introduced by its own work;
- make small adjacent correctness fixes uncovered by new tests;
- commit green checkpoints directly to `main`;
- continue through several substeps of one coherent milestone.

Do not stop merely to ask about:

- private helper names;
- test-file organization;
- ordinary refactoring;
- obvious comment/document updates;
- whether to fix a regression caused by the current work;
- routine choices already constrained by existing architecture.

## 15. Stop conditions

Stop and ask the user when a task would require one of the following:

- secrets, credentials or account changes;
- package/release publication;
- destructive external actions;
- force-push/history rewrite;
- deleting or overwriting unrelated user work;
- adding arbitrary script/macro/plugin execution that changes the security boundary;
- copying historical copyrighted source/manual material rather than clean-room behavior;
- a materially incompatible public API/file-format choice not resolved by spec/docs;
- a historical ambiguity that would produce significantly different user-visible behavior and cannot be classified safely as a modern policy;
- a risky change that cannot be validated locally or by CI after reasonable diagnosis.

## 16. Definition of done

A work item is complete only when all applicable conditions are met:

- behavior matches the task and documented compatibility contract;
- important edge cases have regression tests;
- architecture invariants remain intact;
- topology/undo/marker/block implications were audited when structural text changed;
- `spec/editor-v1.md` is updated when portable behavior changed;
- shared conformance cases are added or updated when the behavior can be expressed portably;
- English and German docs are updated in parallel where appropriate;
- the compatibility matrix reflects the new status;
- Release build succeeds;
- tests pass;
- `git diff --check` is clean;
- no generated binaries/temp files are committed;
- the change is committed as a coherent checkpoint;
- GitHub Actions is green when available.

## 17. Recommended short Codex prompt

Once this file is present, a continuation prompt can be intentionally short:

> Continue autonomously with the next unfinished V1 release-gate item from AGENTS.md. Inspect the current repository first. Preserve clean-room behavior, architecture invariants and bilingual documentation. Add regression tests, avoid premature optimization, commit a green checkpoint directly to main, verify GitHub Actions, and stop only for a documented stop condition.
