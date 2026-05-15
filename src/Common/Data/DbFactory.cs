using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace Common.Data;

/// <summary>
/// DB 工厂 — 从 IConfiguration 读取 Database:{name}，按名懒加载 DapperHelper
/// </summary>
public static class DbFactory
{
    private static readonly ConcurrentDictionary<string, DapperHelper> _dbs = new();
    private static IConfiguration? _config;

    public static void Initialize(IConfiguration config) => _config = config;

    public static DapperHelper Get(string name)
    {
        return _dbs.GetOrAdd(name, key =>
        {
            var connStr = _config?[$"Database:{key}"]
                ?? throw new InvalidOperationException($"未配置 Database:{key}");

            return new DapperHelper(new DapperContext(connStr));
        });
    }
}
