using Common.Configs;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Sdk.Adapters;

/// <summary>
/// 阿里云 OCR SDK 适配器
/// 需安装 NuGet: AlibabaCloud.SDK.Ocr_api20210707
/// </summary>
public class AlibabaOcrAdapter : ISdkAdapter
{
    private readonly ILogger<AlibabaOcrAdapter> _logger;

    public AlibabaOcrAdapter(ILogger<AlibabaOcrAdapter> logger)
    {
        _logger = logger;
    }

    public void Initialize(TpsServiceConfigDO serviceConfig)
    {
        var cfg = serviceConfig.GetConfig<AlibabaOcrConfig>();
        // 安装 AlibabaCloud.SDK.Ocr_api20210707 后：
        // var client = new AlibabaCloud.SDK.Ocr_api20210707.Client(
        //     new AlibabaCloud.OpenApiClient.Models.Config { ... });

        _logger.LogInformation("[AlibabaOcr] SDK 初始化完成 Endpoint={Endpoint}", cfg.Endpoint);
    }

    public async Task<List<string>> GetKeywordsListAsync(string url)
    {
        _logger.LogInformation("[AlibabaOcr] 识别图片 Url={Url}", url);

        // 安装 AlibabaCloud.SDK.Ocr_api20210707 后取消注释调用代码

        await Task.CompletedTask;
        throw new NotImplementedException(
            "阿里云 OCR SDK 未安装。请安装 NuGet 包 AlibabaCloud.SDK.Ocr_api20210707 并取消注释调用代码。");
    }
}
