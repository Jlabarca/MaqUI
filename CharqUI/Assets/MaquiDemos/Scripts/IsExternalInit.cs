// Required for 'record struct' positional parameters (init setters) under .NET Standard 2.1.
// Safe to have in multiple assemblies — the compiler uses whichever it finds first.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
