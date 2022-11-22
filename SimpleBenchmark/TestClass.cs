namespace SimpleBenchmark;

public class TestClass
{
    [Params(10, 100, 1000, 5000)] 
    public int CoCount { get; set; }
    //读写测试
    [Params(false, true)] public bool TestRead { get; set; } = false;
    
    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(500 * 1024)]
    [Arguments(1024 * 1024)]
    [CaseBaseSetup("BenchmarkInsertBefore")]
    [CaseBaseCleanup("BenchmarkInsertAfter")]
    //测试insert在不同大小下的读写性能
    public Task BenchmarkInsert(int dataSize) //插入 清表,确保没有老数据
    {
        //1.写入--benchmark.dotnet动态修改dataSize
        //prometheus记录BenchmarkInsert写入次数+1
        //2.更具TestRead确定是否调用读取ReadResult--benchmark.dotnet调用
        //prometheus记录BenchmarkInsert读取次数+1
        // Env.GetMetric("BenchmarkInsert").Inc(1);
        Console.WriteLine("BenchmarkInsert");
        return Task.CompletedTask;
    }

    [Benchmark]
    [Arguments(10 * 1024)]
    [Arguments(100 * 1024)]
    [Arguments(500 * 1024)]
    [Arguments(1024 * 1024)]
    //测试replace在不同大小下的读写性能
    public Task BenchmarkReplace(int dataSize) //全量替换 清表,重新构造数据
    {
        return Task.CompletedTask;
    }
    //
    // [Benchmark]
    // [Arguments(10, 100 * 1024)]
    // [Arguments(1 * 1024, 100 * 1024)]
    // [Arguments(10 * 1024, 100 * 1024)]
    // [Arguments(100 * 1024, 100 * 1024)]
    // [Arguments(10, 1024 * 1024)]
    // [Arguments(1 * 1024, 1024 * 1024)]
    // [Arguments(10 * 1024, 1024 * 1024)]
    // [Arguments(100 * 1024, 1024 * 1024)]
    // //测试增量更新在不同大小下的读写性能
    // public void BenchmarkIncUpdate(int dataSize, int totalSize) //增量更新 清表,重新构造数据
    // {
    // }
    //
    // [Benchmark]
    // [Arguments(10, 1)]
    // [Arguments(100, 1)]
    // [Arguments(1024, 1)]
    // //测试key大小对读写性能的影响
    // public void BenchmarkInsertKeySize(int keySize, int totalSize) //增量更新 清表,重新构造数据
    // {
    // }
    public Task BenchmarkInsertBefore(int dataSize)
    {
        Console.WriteLine("BenchmarkInsertBefore");
        return Task.CompletedTask;
    }
    public Task BenchmarkInsertAfter(int dataSize)
    {
        Console.WriteLine("BenchmarkInsertAfter");
        return Task.CompletedTask;
    }
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine("GlobalSetup");
    }
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        Console.WriteLine("GlobalCleanup");
    }
}