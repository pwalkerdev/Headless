﻿// ReSharper disable once CheckNamespace
namespace Headless.Framework
{
    // TODO: Haven't really decided yet whether I want this in just a 'shared' folder or in another project entirely. So this is the temporary home of code that can be referenced from inside scripts

    public static class ObjectExtensions
    {
#if NET8_0_OR_GREATER
        public static void Dump(this object? value)
        {
            var str = value?.ToString() ?? "(null)";
            Console.WriteLine("|> " + string.Join("\r\n--", str.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None)));
        }
#endif

#if NET48_OR_GREATER
        public static void Dump(this object value)
        {
            throw new System.NotImplementedException();
        }
#endif
    }
}
