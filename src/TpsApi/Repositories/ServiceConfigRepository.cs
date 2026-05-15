using Common.DI;
using Common.Models;

namespace TpsApi.Repositories;

public class ServiceConfigRepository : BaseRepository, IBaseAutofac
{
    public async Task<TpsServiceConfigDO?> GetByServiceCodeAsync(string serviceCode)
        => await MainDb.GetFirstOrDefaultAsync<TpsServiceConfigDO>(
            "SELECT * FROM tps_service_config WHERE service_code = @ServiceCode AND enabled = 1 AND is_deleted = 0",
            new { ServiceCode = serviceCode });
}
