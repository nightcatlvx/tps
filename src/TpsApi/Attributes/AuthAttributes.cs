namespace TpsApi.Attributes;

/// <summary>
/// 标记方法是否需要授权
/// true = 调用前由对应 IServiceAuth 实现自动注入 Token
/// false = 跳过授权注入，用于获取 Token 的方法本身
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class NeedAuthAttribute(bool required = true) : Attribute
{
    public bool Required { get; } = required;
}
