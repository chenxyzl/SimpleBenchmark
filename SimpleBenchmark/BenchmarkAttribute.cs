using System.Runtime.CompilerServices;

namespace SimpleBenchmark;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ParamsAttribute : Attribute
{
    public object[] Values { get; }

    // CLS-Compliant Code requires a constructor without an array in the argument list
    public ParamsAttribute() => Values = new object[0];

    public ParamsAttribute(params object[] values)
    {
        if (values.Length <= 0) throw new Exception("Params must have param");
        Values = values;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ArgumentsAttribute : Attribute
{
    public object[] Values { get; }

    // CLS-Compliant Code requires a constructor without an array in the argument list
    public ArgumentsAttribute() => Values = new object[0];

    public ArgumentsAttribute(params object[] values)
    {
        if (values.Length <= 0) throw new Exception("Arguments must have param");
        Values = values;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class BenchmarkAttribute : Attribute
{
#pragma warning disable CS8618
    public BenchmarkAttribute([CallerLineNumber] int sourceCodeLineNumber = 0,
#pragma warning restore CS8618
        [CallerFilePath] string sourceCodeFile = "")
    {
        SourceCodeLineNumber = sourceCodeLineNumber;
        SourceCodeFile = sourceCodeFile;
    }

    public string Description { get; set; }

    public bool Baseline { get; set; }

    public int OperationsPerInvoke { get; set; } = 1;

    public int SourceCodeLineNumber { get; }

    public string SourceCodeFile { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public class GlobalSetupAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class GlobalCleanupAttribute : Attribute
{
}


[AttributeUsage(AttributeTargets.Method)]
public class CaseBaseAttribute : Attribute
{
    public string MethodName { get; }

    public CaseBaseAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class CaseBaseSetupAttribute : CaseBaseAttribute
{
    public CaseBaseSetupAttribute(string methodName) : base(methodName)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class CaseBaseCleanupAttribute : CaseBaseAttribute
{
    public CaseBaseCleanupAttribute(string methodName) : base(methodName)
    {
    }
}