using Common.Models;

namespace TpsApi.Client.Sdk;

/// <summary>
/// SDK Adapter 初始化接口
/// </summary>
public interface ISdkAdapter
{
    void Initialize(TpsServiceConfigDO serviceConfig);
}
