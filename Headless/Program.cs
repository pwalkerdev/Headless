var script = new System.Text.StringBuilder();

switch (args.Length)
{
    case 1:
        script.Append(args[0]);
        break;
    case 2:
        if (args[0] == "stream" && args[1] is { Length: > 0 } token)
        {
            while (Console.ReadLine() is { } ln && ln != token)
                script.AppendLine(ln);

            // Removes the token from the console output... Nothing really wrong with it being there but idk it looks like it doesn't belong.
            ConsoleExtensions.BlankOutLastLine();
        }
        break;
}

if (script.Length > 0)
{
    var headless = new Headless.Core.HeadlessService();
    var compileResult = await headless.ResolveCompiler("CSharp").Compile(script.ToString());
    var invokeResult = await  headless.ResolveInvoker("CSharp").Run(compileResult);
}
else
    Console.WriteLine("Failed to receive script from caller!");