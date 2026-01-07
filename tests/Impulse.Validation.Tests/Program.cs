namespace Impulse.Validation.Tests;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("Impulse.Validation Tests");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        ZodSchemaGeneratorTests.RunAll();

        return TestRunner.Report();
    }
}
