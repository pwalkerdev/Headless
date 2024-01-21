#if NET8_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace Headless.CSharp;

// ReSharper disable twice NotAccessedPositionalProperty.Global
public record ScriptParameterDescriptor(int Order, string? Name, Type? Type);
#endif