namespace Impulse.Core.Tests;

public static class Program
{
    public static int Main()
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("Impulse.Core Tests");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        ComponentPathConventionTests.RunAll();

        return TestRunner.Report();
    }
}
