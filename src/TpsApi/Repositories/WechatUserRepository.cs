using Common.DI;
using Common.Models;

namespace TpsApi.Repositories;

public class WechatUserRepository : BaseRepository, IBaseAutofac
{
    public async Task<WechatUserDO?> GetByMobileAsync(string mobile)
        => await MainDb.GetFirstOrDefaultAsync<WechatUserDO>(
            "SELECT * FROM tps_wechat_user_data WHERE mobile = @Mobile LIMIT 1",
            new { Mobile = mobile });

    public async Task<int> InsertAsync(string mobile, string wxid)
        => await MainDb.ExecuteAsync(
            "INSERT INTO tps_wechat_user_data (mobile, wxid, create_time) VALUES (@Mobile, @Wxid, @Now)",
            new { Mobile = mobile, Wxid = wxid, Now = DateTime.Now });
}
