using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Common.Data;

namespace TpsApi.Repositories;

/// <summary>
/// 业务仓储基类
/// </summary>
public abstract class BaseRepository
{
    protected DapperHelper MainDb    => GetDb("MainDB");
    protected DapperHelper OnlyReadDb => GetDb("OnlyReadMainDB");

    protected static DapperHelper GetDb(string dbName) => DbFactory.Get(dbName);

    /// <summary>
    /// 从 [Table] 解析表名
    /// </summary>
    protected static string ResolveTableName<T>() where T : class
    {
        var attr = typeof(T).GetCustomAttribute<TableAttribute>();
        return attr?.Name ?? typeof(T).Name;
    }
}
