using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace Common.Data;

/// <summary>
/// Dapper 封装 — 干净直接的数据库操作方法
/// </summary>
public class DapperHelper
{
    private readonly string _connString;

    public DapperHelper(DapperContext context)
    {
        _connString = context.ConnectionString;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connString);

    // ── 查询 ────────────────────────────────────────────────────

    /// <summary>
    /// 查询单条记录，无结果返回 null
    /// </summary>
    public async Task<T?> GetFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    /// <summary>
    /// 查询列表
    /// </summary>
    public async Task<IEnumerable<T>> GetListAsync<T>(string sql, object? param = null)
    {
        using var db = CreateConnection();
        return await db.QueryAsync<T>(sql, param);
    }

    /// <summary>
    /// 分页查询，返回 (列表, 总数)
    /// </summary>
    public async Task<(IEnumerable<T> List, int Total)> GetPagedAsync<T>(
        string sql, object? param, int pageIndex, int pageSize, string countSql = "")
    {
        using var db = CreateConnection();
        if (string.IsNullOrEmpty(countSql))
            countSql = $"SELECT COUNT(1) FROM ({sql}) AS _cnt";
        var total = await db.ExecuteScalarAsync<int>(countSql, param);
        var list = await db.QueryAsync<T>($"{sql} LIMIT {(pageIndex - 1) * pageSize}, {pageSize}", param);
        return (list, total);
    }

    // ── 写入 ────────────────────────────────────────────────────

    /// <summary>
    /// 插入单条记录，返回自增ID（无自增ID时返回1）
    /// </summary>
    public async Task<long> InsertAsync<T>(string tableName, T entity)
    {
        using var db = CreateConnection();

        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "id")
            .ToList();

        var columns = string.Join(", ", props.Select(p => $"`{p.Name}`"));
        var values = string.Join(", ", props.Select(p => $"@{p.Name}"));
        var sql = $"INSERT INTO `{tableName}` ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";

        var id = await db.ExecuteScalarAsync<long>(sql, entity);

        // 回写自增ID
        typeof(T).GetProperty("id")?.SetValue(entity, Convert.ToUInt32(id));

        return id > 0 ? id : 1;
    }

    /// <summary>
    /// 批量插入，返回是否全部成功
    /// </summary>
    public async Task<bool> BatchInsertAsync<T>(string tableName, IEnumerable<T> entities)
    {
        using var db = CreateConnection();
        var list = entities.ToList();
        if (list.Count == 0) return false;

        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "id")
            .ToList();

        var columns = string.Join(", ", props.Select(p => $"`{p.Name}`"));

        if (list.Count == 1)
        {
            var singleColumns = string.Join(", ", props.Select(p => $"`{p.Name}`"));
            var singleValues = string.Join(", ", props.Select(p => $"@{p.Name}"));
            var singleSql = $"INSERT INTO `{tableName}` ({singleColumns}) VALUES ({singleValues})";
            return await db.ExecuteAsync(singleSql, list[0]) == 1;
        }

        var valueRows = new List<string>();
        var parameters = new DynamicParameters();

        for (int i = 0; i < list.Count; i++)
        {
            var rowParams = props.Select(p => $"@p{p.Name}{i}");
            valueRows.Add($"({string.Join(", ", rowParams)})");
            foreach (var prop in props)
                parameters.Add($"p{prop.Name}{i}", prop.GetValue(list[i]));
        }

        var sql = $"INSERT INTO `{tableName}` ({columns}) VALUES {string.Join(", ", valueRows)}";
        var rows = await db.ExecuteAsync(sql, parameters);
        return rows == list.Count;
    }

    // ── 更新 / 删除 ─────────────────────────────────────────────

    /// <summary>
    /// 执行 UPDATE / DELETE / 任意 SQL，返回影响行数
    /// </summary>
    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var db = CreateConnection();
        return await db.ExecuteAsync(sql, param);
    }

    /// <summary>
    /// 按主键更新实体（排除 id 字段，WHERE id = @id）
    /// </summary>
    public async Task<int> UpdateByIdAsync<T>(string tableName, T entity)
    {
        using var db = CreateConnection();

        var props = typeof(T).GetProperties()
            .Where(p => p.Name != "id")
            .ToList();

        var sets = string.Join(", ", props.Select(p => $"`{p.Name}` = @{p.Name}"));
        var sql = $"UPDATE `{tableName}` SET {sets} WHERE id = @id";

        return await db.ExecuteAsync(sql, entity);
    }

    /// <summary>
    /// 按主键逻辑删除（SET is_deleted = 1）
    /// </summary>
    public async Task<int> SoftDeleteByIdAsync(string tableName, uint id)
    {
        using var db = CreateConnection();
        return await db.ExecuteAsync(
            $"UPDATE `{tableName}` SET is_deleted = 1 WHERE id = @Id",
            new { Id = id });
    }

    /// <summary>
    /// 按主键物理删除
    /// </summary>
    public async Task<int> DeleteByIdAsync(string tableName, uint id)
    {
        using var db = CreateConnection();
        return await db.ExecuteAsync(
            $"DELETE FROM `{tableName}` WHERE id = @Id",
            new { Id = id });
    }

    /// <summary>
    /// 执行标量查询（如 COUNT / SUM）
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<T>(sql, param);
    }
}
