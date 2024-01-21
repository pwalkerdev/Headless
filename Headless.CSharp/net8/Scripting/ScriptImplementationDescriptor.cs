// ReSharper disable once CheckNamespace
namespace Headless.CSharp.Scripting;

public record ScriptImplementationDescriptor(MethodBodyImplementation EntryPointBodyType, MethodParameter[] ParameterDescriptors);