namespace Impulse.CodeGen.Tests;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("Impulse.CodeGen Tests");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        TypeScriptGeneratorTests.RunAll();

        return TestRunner.Report();
    }
}
