# Tps — 声明式第三方 API 代理网关

设计思想类似 Java 的 **Retrofit**——在接口上声明 Attribute 驱动完整调用管道，把限流、签名、授权、弹性策略从业务代码中剥离。在此之上内置了付费API/SDK集中管理、令牌桶限流、熔断保护，对标的其实是 Retrofit + Resilience4j 的组合。

## 三种调用模式

**HTTP + 签名（有道翻译）**

```csharp
[ServiceCode("youdao")]                          // 服务标识
[FuncCode("youdao_translate")]                   // 功能标识
[ApiMethod(Method = "POST", ContentType = "application/x-www-form-urlencoded")]
[NeedSign(true)]                                 // 自动根据服务标识查找签名器签名 
[RetryPolicy(RetryCount = 3, WaitSeconds = [1, 2, 3])]
[CircuitBreaker(BreakAfterFaults = 5, BreakDurationSeconds = 30)]
[Timeout(TimeoutMs = 10000)]
Task<YoudaoTranslateResult> TranslateAsync([Body] YoudaoTranslateParam dto);
```

**HTTP + 授权（企业微信）**

```csharp
[ServiceCode("qywx")]                            // 服务标识
[FuncCode("qywx_send_message")]                  // 功能标识
[ApiMethod(Method = "POST")]
[NeedAuth(true)]                                 // 自动根据服务标识查找鉴权器（Redis 缓存，401 自动刷新）
[Timeout(TimeoutMs = 5000)]
Task<QywxSendMessageResult> SendMessageAsync([Body] QywxSendMessageParam dto);
```

**SDK 调用（阿里云 OCR）**

```csharp
[ServiceCode("alibaba_ocr")]                     // 服务标识
[FuncCode("alibaba_ocr_keywords")]               // 功能标识
[SdkMethod(typeof(AlibabaOcrAdapter))]           // 走 SDK，不走 HTTP（限流依然生效）
Task<List<string>> GetKeywordsListAsync(string pic_url);
```

三种模式共享同一条管道：**限流 → 签名/授权 → 弹性策略（超时→熔断→重试）→ 发送**。接入新服务只需在接口上加方法、插入 DB 配置，代理核心零改动。

## 系统架构

```
Controller  →  Service  →  IExternalClient（声明式接口）
                                ↓
                      ThirdPartyClientProxy（DispatchProxy 拦截）
                                ↓
         ┌──────────┬───────────┼───────────┬──────────┐
         ↓          ↓           ↓           ↓          ↓
    解析Attribute  查DB配置   令牌桶限流   构建请求   弹性策略
                  (服务/功能/   (Redis      (Path/    (超时→
                   限流规则)    +Lua)       Query/     熔断→
                                           Body)      重试)
         ↓          ↓           ↓           ↓          ↓
         └──────────┴───────────┼───────────┴──────────┘
                                ↓
              SignHandler ──→ AuthHandler ──→ HttpClient / SDK
              (签名匹配)      (Token匹配)
```

**运行逻辑**：调用接口方法 → DispatchProxy 拦截 → 反射解析 Attribute → 查库拿服务地址/密钥/限流规则 → Redis 令牌桶检查 → 构建 HTTP 请求 → 签名处理器匹配服务标识执行签名 → 授权处理器匹配服务标识注入 Token → Polly 策略链保护 → 发送 → 反序列化返回。

## Attribute 一览

| Attribute | 用途 | 作用域 | 可省略 |
|-----------|------|--------|--------|
| `[ServiceCode("xxx")]` | 服务标识，关联库中的服务地址和密钥 | 接口 / 方法 | 否 |
| `[FuncCode("xxx")]` | 功能标识，关联库中的路由和限流规则 | 方法 | 否 |
| `[ApiMethod(Method, ContentType)]` | HTTP 方法和内容类型，默认 GET + json | 方法 | 是 |
| `[ApiPath("/xxx/{p}")]` | 请求路径，支持 `{占位符}`，不填则读库 | 方法 | 是 |
| `[PathParam("p")]` | 绑定路径占位符，不填则用参数名 | 参数 | 是 |
| `[Body]` | 标记参数序列化为请求体 | 参数 | 是 |
| `[Query]` | 标记参数拼到 QueryString | 参数 | 是 |
| `[NeedAuth(true/false)]` | 是否需要鉴权注入 Token | 接口 / 方法 | 是（默认 false） |
| `[NeedSign(true/false)]` | 是否需要请求签名 | 接口 / 方法 | 是（默认 false） |
| `[RetryPolicy(RetryCount, WaitSeconds)]` | 重试次数 + 等待时间序列或指数退避 | 接口 / 方法 | 是 |
| `[CircuitBreaker(BreakAfterFaults, BreakDurationSeconds)]` | 连续失败 N 次后熔断 M 秒 | 接口 / 方法 | 是 |
| `[Timeout(TimeoutMs)]` | 超时毫秒数 | 接口 / 方法 | 是（默认 5000） |
| `[SdkMethod(typeof(Adapter))]` | 走 SDK 调用而非 HTTP | 方法 | 是 |

## 配置体系

三张配置表，库配分离：

| 表 | 内容 | 关键字段 |
|----|------|---------|
| `tps_service_config` | 服务地址 + 密钥 | `service_code`（服务标识）, `base_url`, `config_json` |
| `tps_func_config` | 功能路由 | `func_code`（功能标识）, `service_id`, `path` |
| `tps_func_rate_limit_rule` | 限流规则 | `func_id`, `window_seconds`, `max_requests`, `burst_per_second` |

服务标识和功能标识是代码与配置之间的关联键，必须严格一致。

令牌产生速率 = `max_requests / window_seconds`，`burst_per_second` 控制桶容量允许一定突发。

## 使用注意

### 限流实现切换

默认 Redis 分布式版，无 Redis 或单实例可切内存版。在 `ThirdPartyClientExtension.cs` 中：

```csharp
// 分布式（默认）
services.AddSingleton<IRateLimiter, RedisRateLimiter>();

// 内存版
services.AddSingleton<LocalTokenBucketStore>();
services.AddSingleton<IRateLimiter, LocalRateLimiter>();
```

### Token 死循环

获取 Token 的方法必须 `[NeedAuth(false)]`。

### Polly 缓存作用域

熔断器状态以 static 缓存，单进程内跨请求共享。多实例部署时每个实例独立计数——如需跨实例熔断需要分布式方案。

### 接入新服务步骤

1. DB 插入服务配置 → 功能配置 →（可选）限流规则
2. `IExternalClient` 加方法 + Attribute
3. 签名/授权/SDK 有需要就实现对应接口（框架自动扫描，不用改 DI）
4. 写 Service + Controller

## 项目结构

```
src/
├── Common/          # Dapper、Redis、NLog、DI 基础设施
└── TpsApi/          # Web API
    ├── Attributes/  # 12 个声明式 Attribute
    ├── Client/      # 代理核心、Auth、Sign、RateLimit、Sdk
    ├── Controllers/
    ├── Services/
    ├── Repositories/
    └── Extensions/  # DI 注册、TraceId 中间件
```

## 技术栈与依赖

| 组件 | 用途 |
|------|------|
| DispatchProxy | 动态代理拦截 |
| Polly | 超时 / 熔断 / 重试 |
| Dapper + MySqlConnector | 轻量 ORM |
| Autofac | DI 容器，IBaseAutofac 自动扫描 |
| StackExchange.Redis | Token 缓存 + 分布式令牌桶 |
| NLog | 日志 |

## 快速开始

```bash
dotnet restore tps.slnx
# 改 appsettings.json 的 DB 和 Redis 连接
# 执行 init.sql 建表 + 示例数据
dotnet run --project src/TpsApi
```
