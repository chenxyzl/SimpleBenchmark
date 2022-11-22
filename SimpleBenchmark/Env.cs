using System.Net;
using CommandLine;
using Prometheus;

namespace SimpleBenchmark;

public static class Env
{
    public static Options Config { get; private set; } = null!;

    public class Options
    {
        [Option('c', "ConcurrentCount", HelpText = "并发数", Default = 1)]
        public int ConcurrentCount { get; private set; }

        [Option('t', "CaseRunTime", HelpText = "每个case运行时长", Default = 1)]
        public int CaseRunTime { get; private set; }


        [Option('m', "Model", HelpText = "模式")]
        public bool Model { get; private set; } = false;
    }

    public static void Init(string[] args)
    {
        InitParams(args);
        InitMetric();
        InitMongo();
    }

    static void InitParams(string[] args)
    {
        Config = Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
        {
            if (o.ConcurrentCount > 0)
            {
                Console.WriteLine($"Current ConcurrentCount: -c {o.ConcurrentCount}");
            }
            else
            {
                throw new Exception("ConcurrentCount must bigger than 0; please run with params[-c $num]");
            }
        }).Value;
    }

    private static readonly Dictionary<string, ICounter> MetricsDic = new();
    private static MetricServer? _server;

    static void InitMetric()
    {
        var port = 9101;
        // Start the metrics server on your preferred port number.
        _server = new MetricServer(port: port);

        try
        {
            // On .NET Framework, starting the server requires either elevation to Administrator or permission configuration.
            _server.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Failed to start metric server: {ex.Message}");
            Console.WriteLine(
                "You may need to grant permissions to your user account if not running as Administrator:");
            Console.WriteLine($"netsh http add urlacl url=http://+:{port}/metrics user=DOMAIN\\user");
            return;
        }

        // Generate some sample data from fake business logic.
        // var recordsProcessed =
        //     Metrics.CreateCounter("sample_records_processed_total", "Total number of records processed.");
        //
        // _ = Task.Run(async delegate
        // {
        //     while (true)
        //     {
        //         // Pretend to process a record approximately every second, just for changing sample data.
        //         recordsProcessed.Inc();
        //
        //         await Task.Delay(TimeSpan.FromSeconds(1));
        //     }
        // });

        // Metrics published in this sample:
        // * built-in process metrics giving basic information about the .NET runtime (enabled by default)
        // * the sample counter defined above
        Console.WriteLine($"Open http://localhost:{port}/metrics in a web browser.");
        Console.WriteLine("Press enter to exit.");
    }


    public static ICounter GetMetric(string fullName)
    {
        if (_server == null)
        {
            throw new Exception("_server 没有初始化");
        }

        if (!MetricsDic.TryGetValue(fullName, out var gauge))
        {
            gauge = Metrics.CreateCounter(fullName, fullName);
            MetricsDic.Add(fullName, gauge);
        }

        return gauge;
    }


    static void InitMongo()
    {
    }

    public static void Stop()
    {
        _server?.Stop();
    }
}