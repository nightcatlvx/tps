namespace Common.Data;

/// <summary>
/// Dapper 连接配置
/// </summary>
public class DapperContext
{
    public string ConnectionString { get; }

    public DapperContext(string connectionString)
    {
        ConnectionString = connectionString;
    }
}
