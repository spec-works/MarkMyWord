// Polyfill for C# 9 init-only properties in .NET Standard 2.1
#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
