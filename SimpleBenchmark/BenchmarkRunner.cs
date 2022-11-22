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

    public static async Task Run<T>() where T : class
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
        //检查CaseSetupAttribute
        var caseSetupMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseBaseSetupAttribute>().Any()).ToArray();
        CheckCaseMethod<T, CaseBaseSetupAttribute>(caseSetupMethods);

        //检查CaseCleanupAttribute
        var caseCleanMethods = typeof(T).GetMethods()
            .Where(typ => typ.GetCustomAttributes(true).OfType<CaseBaseCleanupAttribute>().Any()).ToArray();
        CheckCaseMethod<T, CaseBaseCleanupAttribute>(caseSetupMethods);

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
                await CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods,
                    $"[{string.Join("-", record.ToArray())}]");
            }
        }
        else
        {
            await CallAllMethod(obj, allBenchmarkMethods, caseSetupMethods, caseCleanMethods, "[]");
        }

        //再调用Cleanup
        foreach (var method in allBenchmarkCleanMethods)
        {
            method.Invoke(obj, null);
        }
    }

    static void CheckCaseMethod<T, A>(MethodInfo[] caseMethods) where A : CaseBaseAttribute
    {
        foreach (var caseMethod in caseMethods)
        {
            var caseAttributes = caseMethod.GetCustomAttributes<A>().ToArray();
            foreach (var caseAttribute in caseAttributes)
            {
                var methodInfo = typeof(T).GetMethod(caseAttribute.MethodName);
                if (methodInfo == null)
                    throw new Exception($"Method:{caseAttribute.MethodName} not found");
                if (methodInfo.GetParameters().Length != caseMethod.GetParameters().Length)
                    throw new Exception(
                        $"Method:{caseAttribute.MethodName}-{caseMethod.Name} param not same");
                for (int i = 0; i < methodInfo.GetParameters().Length; i++)
                {
                    if (methodInfo.GetParameters()[i].ParameterType.FullName !=
                        caseMethod.GetParameters()[i].ParameterType.FullName)
                        throw new Exception(
                            $"Method:{caseAttribute.MethodName}-{caseMethod.Name} param type not same");
                }
            }
        }
    }

    static async Task CallAllMethod<T>(T obj, MethodInfo[] allBenchmarkMethods, MethodInfo[] caseSetupMethods,
        MethodInfo[] caseCleanMethods, string paramPrefix) where T : class
    {
        foreach (var method in allBenchmarkMethods)
        {
            var paramsArray = method.GetCustomAttributes(true).OfType<ArgumentsAttribute>().ToArray();
            if (paramsArray.Length > 0)
            {
                foreach (var argumentsAttribute in paramsArray)
                {
                    await RunSingleMethod(obj, method, argumentsAttribute.Values, paramPrefix);
                }
            }
            else
            {
                await RunSingleMethod(obj, method, new object[] { }, paramPrefix);
            }
        }
    }

    static async Task RunSingleMethod<T>(T obj, MethodInfo method, object[] values, string paramPrefix) where T : class
    {
        CaseBaseSetupAttribute[] caseSetupAttributes =
            method.GetCustomAttributes<CaseBaseSetupAttribute>(true).ToArray();
        CaseBaseCleanupAttribute[] caseCleanupAttributes =
            method.GetCustomAttributes<CaseBaseCleanupAttribute>(true).ToArray();
        var str = $"{method.Name}:{paramPrefix}:{string.Join("-", values.ToArray())}";
        Console.WriteLine($"{str}: 开始...");
        Console.WriteLine($"{str}: 初始化资源...");
        foreach (var caseSetup in caseSetupAttributes)
        {
            Task v = (Task)obj.GetType().GetMethod(caseSetup.MethodName)!.Invoke(obj, values)!;
            await v;
        }

        Console.WriteLine($"{str}: 初始化资源完成...");
        Console.WriteLine($"{str}: 运行中...");
        var start = Stopwatch.StartNew();
        while (true)
        {
            Task v = (Task)method.Invoke(obj, values)!;
            await v;
            //执行Env.Config.CaseRunTime秒这个方法
            if (start.ElapsedMilliseconds >= Env.Config.CaseRunTime * 1000) break;
        }

        Console.WriteLine($"{str}: 结束...");
        Console.WriteLine($"{str}: 清理资源中...");
        foreach (var caseCleanup in caseCleanupAttributes)
        {
            Task v = (Task)obj.GetType().GetMethod(caseCleanup.MethodName)!.Invoke(obj, values)!;
            await v;
        }

        Console.WriteLine($"{str}: 完成...");
    }
}