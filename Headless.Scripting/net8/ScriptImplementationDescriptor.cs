﻿#if NET8_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace Headless.Scripting;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record ScriptImplementationDescriptor(ScriptEntryPointType EntryPointType, ScriptParameterDescriptor[] ParameterDescriptors);
#endif