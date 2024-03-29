// ReSharper disable once CheckNamespace
namespace Headless.Targetting.CSharp.Scripting;

public record ScriptImplementationDescriptor(MethodBodyImplementation EntryPointBodyType, MethodParameter[] ParameterDescriptors);