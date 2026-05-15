namespace TpsApi.Attributes;

/// <summary>
/// 标记方法是否需要签名
/// 打在接口上 = 所有方法都需要签名
/// 打在方法上 = 仅该方法需要签名
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class NeedSignAttribute : Attribute
{
    public bool Required { get; }

    public NeedSignAttribute(bool required = true)
    {
        Required = required;
    }
}
