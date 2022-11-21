using System.Diagnostics;
using System.Reflection;

namespace SimpleBenchmark;

class ParamGroup
{
    internal class Param
    {
        public Param(PropertyInfo propertyInfo, object val)
        {
            PropertyInfo = propertyInfo;
            Val = val;
        }

        public PropertyInfo PropertyInfo { get; }
        public object Val { get; }
    }

    public List<Param> Params { get; } = new();

    public ParamGroup(PropertyInfo propertyInfo, object val)
    {
        Params.Add(new Param(propertyInfo, val));
    }

    public ParamGroup(List<Param> params1, List<Param> params2)
    {
        Params.AddRange(params1);
        Params.AddRange(params2);
    }

    public static ParamGroup operator +(ParamGroup a, ParamGroup b)
    {
        return new ParamGroup(a.Params, b.Params);
    }
}

public sealed class BenchmarkRunner
{
    static List<ParamGroup> DoExchange(List<List<ParamGroup>> arr)
    {
        var len = arr.Count;
        // 当数组大于等于2个的时候
        if (len >= 2)
        {
            // 第一个数组的长度
            var len1 = arr[0].Count;
            // 第二个数组的长度
            var len2 = arr[1].Count;
            // 2个数组产生的组合数
            var lenBoth = len1 * len2;
            //  申明一个新数组,做数据暂存
            var items = new List<ParamGroup>(new ParamGroup[lenBoth]);
            // 申明新数组的索引
            var index = 0;
            // 2层嵌套循环,将组合放到新数组中
            for (var i = 0; i < len1; i++)
            {
                for (var j = 0; j < len2; j++)
                {
                    items[index] = arr[0][i] + arr[1][j];
                    index++;
                }
            }

            // 把已经合并过的数组删除掉，将新组合的数组并到原数组中
            arr.RemoveRange(0, 2);
            var v = new List<List<ParamGroup>>() { items };
            v.AddRange(arr);
            // 执行回调
            return DoExchange(v);
        }

        return arr[0];
    }

    public static void Run<T>() where T : class
    {
        //todo 获取类型符合的属性--自用,就不检查合法性了
        var tempAllBenchmarkParams = typeof(T).GetProperties()
            .Where(typ => typ.GetCustomAttributes(true).OfType<ParamsAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkSetupMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<GlobalSetupAttribute>().Any()).ToArray();
        //todo 获取类型符合的方法,以及方法的参数--自用,就不检查合法性了
        var allBenchmarkCleanMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<GlobalCleanupAttribute>().Any()).ToArray();

        var caseSetupMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseSetupAttribute>().Any()).ToArray();
        var caseCleanMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseCleanupAttribute>().Any()).ToArray();

        //构造对象
        var obj = Activator.CreateInstance<T>();

        //先调用Setup方法
        foreach (var method in allBenchmarkSetupMethods)
        {
            method.Invoke(obj, null);
        }

        var allParams = new List<List<ParamGroup>>();
        foreach (var benchmarkParam in tempAllBenchmarkParams)
        {
            var tParams = new List<ParamGroup>();
            var ps = benchmarkParam.GetCustomAttributes(true).OfType<ParamsAttribute>().ToArray();
            if (ps.Length != 1) throw new Exception("Params not allow AllowMultiple");
            var paramsAttribute = ps[0]; //属性标记不会重复
            foreach (var paramsValue in paramsAttribute.Values)
            {
                tParams.Add(new ParamGroup(benchmarkParam, paramsValue));
            }

            allParams.Add(tParams);
        }

        var paramGroups = DoExchange(allParams);

        if (paramGroups.Count > 0)
        {
            //找到所有的参数
            //一组就是完整的一个参数的case
            foreach (var paramGroup in paramGroups)
            {
                List<string> record = new List<string>(); //记录参数列表--预留后期改为多个参数的支持
                //设置一组参数
                foreach (var param in paramGroup.Params)
                {
                    param.PropertyInfo.SetValue(obj, param.Val);
                    record.Add($"{param.PropertyInfo.Name}:{param.Val}");
                }

                //运行
                CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods,
                    $"[{string.Join("-", record.ToArray())}]");
            }
        }
        else
        {
            CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods, "[]");
        }

        //再调用Cleanup
        foreach (var method in allBenchmarkCleanMethods)
        {
            method.Invoke(obj, null);
        }
    }

    static void CallAllMethod<T>(T obj, MethodInfo[] allBenchmarkMethods, MethodInfo[] caseSetupMethods,
        MethodInfo[] caseCleanMethods, string paramPrefix) where T : class
    {
        foreach (var method in allBenchmarkMethods)
        {
            var paramsArray = method.GetCustomAttributes(true).OfType<ArgumentsAttribute>().ToArray();
            if (paramsArray.Length > 0)
            {
                foreach (var argumentsAttribute in paramsArray)
                {
                    RunSingleMethod(obj, method, argumentsAttribute.Values, caseSetupMethods, caseCleanMethods,
                        paramPrefix);
                }
            }
            else
            {
                RunSingleMethod(obj, method, new object[] { }, caseSetupMethods, caseCleanMethods, paramPrefix);
            }
        }
    }

    static void RunSingleMethod<T>(T obj, MethodInfo method, object[] values, MethodInfo[] caseSetupMethod,
        MethodInfo[] caseCleanMethod, string paramPrefix) where T : class
    {
        var str = $"paramPrefix:{paramPrefix}:{method.Name}:{string.Join("-", values.ToArray())}";
        foreach (var methodInfo in caseSetupMethod)
        {
            methodInfo.Invoke(obj, new object[] { method.Name });
        }

        Console.WriteLine($"{str}: 开始运行...");
        var start = Stopwatch.StartNew();
        while (true)
        {
            method.Invoke(obj, values);
            //执行5分钟这个方法
            if (start.ElapsedMilliseconds >= Env.Config.CaseRunTime * 1000) break;
        }

        foreach (var methodInfo in caseCleanMethod)
        {
            methodInfo.Invoke(obj, new object[] { method.Name });
        }

        Console.WriteLine($"{str}: 结束运行...");
    }
}