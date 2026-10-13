# C++ implementation lane

This directory is reserved for the native C++ implementation of SASD Editor Toolbox.

Status: **scaffold only**. No C++ editor code is claimed yet.

When implementation begins, treat `../../spec/editor-v1.md` as the portable behavior contract, consume shared cases from `../../conformance/` through a C++ test adapter, keep the reusable core UI-agnostic, use idiomatic modern C++ ownership/value semantics rather than mirroring C# object layout, contain the C++ build system inside this lane, and keep C++ independent from the .NET and Java binaries.

See `../../docs/en/MULTI-LANGUAGE-DEVELOPMENT.md`.
