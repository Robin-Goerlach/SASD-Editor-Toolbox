using System.Runtime.CompilerServices;

// The test assembly may exercise internal staging failure points without
// expanding the production persistence API exposed to editor hosts.
[assembly: InternalsVisibleTo("Sasd.Editor.Core.Tests")]
