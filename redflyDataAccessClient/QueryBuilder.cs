using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace redflyDataAccessClient;

public class QueryBuilder
{
    // Encrypted connection properties
    public string EncryptedDatabaseHost { get; set; } = string.Empty;
    public string EncryptedDatabase { get; set; } = string.Empty;
    public string EncryptedDatabaseUser { get; set; } = string.Empty;
    public string EncryptedDatabasePassword { get; set; } = string.Empty;
    public string EncryptedKey { get; set; } = string.Empty;

    // Redis connection properties
    public string EncryptedRedisServerName { get; set; } = string.Empty;
    public int RedisPortNo { get; set; }
    public string EncryptedRedisPassword { get; set; } = string.Empty;
    public bool RedisUsesSsl { get; set; }
    public string RedisSslProtocol { get; set; } = string.Empty;
    public bool RedisAbortConnect { get; set; }
    public int RedisConnectTimeout { get; set; }
    public int RedisSyncTimeout { get; set; }
    public int RedisAsyncTimeout { get; set; }

    // Query structure
    public string TableName { get; set; } = string.Empty;
    public List<string> SelectColumns { get; set; } = new();
    public List<Condition> WhereConditions { get; set; } = new();
    public List<Join> Joins { get; set; } = new();
    public List<OrderBy> OrderBys { get; set; } = new();

    // Fluent API
    public QueryBuilder From(string encHost, string encDb, string encUser, string encPwd, string encKey)
    {
        EncryptedDatabaseHost = encHost;
        EncryptedDatabase = encDb;
        EncryptedDatabaseUser = encUser;
        EncryptedDatabasePassword = encPwd;
        EncryptedKey = encKey;
        return this;
    }

    public QueryBuilder Cached(
        string encryptedRedisServerName,
        int redisPortNo,
        string encryptedRedisPassword,
        bool redisUsesSsl,
        string redisSslProtocol,
        bool redisAbortConnect,
        int redisConnectTimeout,
        int redisSyncTimeout,
        int redisAsyncTimeout)
    {
        EncryptedRedisServerName = encryptedRedisServerName;
        RedisPortNo = redisPortNo;
        EncryptedRedisPassword = encryptedRedisPassword;
        RedisUsesSsl = redisUsesSsl;
        RedisSslProtocol = redisSslProtocol;
        RedisAbortConnect = redisAbortConnect;
        RedisConnectTimeout = redisConnectTimeout;
        RedisSyncTimeout = redisSyncTimeout;
        RedisAsyncTimeout = redisAsyncTimeout;
        return this;
    }

    public QueryBuilder Table(string table)
    {
        TableName = table;
        return this;
    }

    public QueryBuilder Select(params string[] columns)
    {
        SelectColumns.AddRange(columns);
        return this;
    }

    public QueryBuilder Where(string column, string op, object? value)
    {
        WhereConditions.Add(new Condition { Column = column, Operator = op, Value = value, Logical = "AND" });
        return this;
    }

    public QueryBuilder And(string column, string op, object? value)
    {
        WhereConditions.Add(new Condition { Column = column, Operator = op, Value = value, Logical = "AND" });
        return this;
    }

    public QueryBuilder Or(string column, string op, object? value)
    {
        WhereConditions.Add(new Condition { Column = column, Operator = op, Value = value, Logical = "OR" });
        return this;
    }

    public QueryBuilder Join(string joinType, string table, string leftColumn, string rightColumn)
    {
        Joins.Add(new Join { JoinType = joinType, Table = table, LeftColumn = leftColumn, RightColumn = rightColumn });
        return this;
    }

    public QueryBuilder OrderBy(string column)
    {
        OrderBys.Add(new OrderBy { Column = column, Descending = false });
        return this;
    }

    public QueryBuilder OrderByAsc(string column)
    {
        OrderBys.Add(new OrderBy { Column = column, Descending = false });
        return this;
    }

    public QueryBuilder OrderByDesc(string column)
    {
        OrderBys.Add(new OrderBy { Column = column, Descending = true });
        return this;
    }
}

public class Condition
{
    public string Column { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public object? Value { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Logical { get; set; }
}

public class Join
{
    public string JoinType { get; set; } = "INNER"; // INNER, LEFT, RIGHT, etc.
    public string Table { get; set; } = string.Empty;
    public string LeftColumn { get; set; } = string.Empty;
    public string RightColumn { get; set; } = string.Empty;
}

public class OrderBy
{
    public string Column { get; set; } = string.Empty;
    public bool Descending { get; set; }
}