using Common.DI;
using Common.Models;

namespace TpsApi.Repositories;

public class RateLimitRuleRepository : BaseRepository, IBaseAutofac
{
    public async Task<TpsFuncRateLimitRuleDO?> GetByFuncIdAsync(uint funcId)
        => await MainDb.GetFirstOrDefaultAsync<TpsFuncRateLimitRuleDO>(
            "SELECT * FROM tps_func_rate_limit_rule WHERE func_id = @FuncId AND enabled = 1 AND is_deleted = 0",
            new { FuncId = funcId });
}
