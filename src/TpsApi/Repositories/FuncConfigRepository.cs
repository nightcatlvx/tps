using Common.DI;
using Common.Models;

namespace TpsApi.Repositories;

public class FuncConfigRepository : BaseRepository, IBaseAutofac
{
    public async Task<TpsFuncConfigDO?> GetByFuncCodeAsync(string funcCode)
        => await MainDb.GetFirstOrDefaultAsync<TpsFuncConfigDO>(
            "SELECT * FROM tps_func_config WHERE func_code = @FuncCode AND enabled = 1 AND is_deleted = 0",
            new { FuncCode = funcCode });
}
