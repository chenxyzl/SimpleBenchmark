namespace SimpleBenchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        // Env.Init(args);
        RunBenchmark();
        // Env.Stop();
    }

    private static void RunBenchmark()
    {
        Console.WriteLine("---Benchmark---");
        BenchmarkRunner.Run<SimpleBenchmark.TestClass>();
        Console.WriteLine("---Benchmark完成---");
    }
}