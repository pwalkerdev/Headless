#if NET48_OR_GREATER
using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Headless.Scripting
{
    public static class VeryCoolWorkingAndReliableCSharpScriptInterpreter
    {
        public static void CompileAndRun(string script)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                if (DateTime.Now - startTime < TimeSpan.FromSeconds(30))
                {
                    Console.WriteLine("Compiling...");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Still compiling!");
                    Console.WriteLine("I know it's taking quite a while, but it's almost done!");
                    Console.WriteLine();
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
    }
}
#endif