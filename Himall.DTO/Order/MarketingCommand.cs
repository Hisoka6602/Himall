using Himall.DTO.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 营销活动命令
    /// </summary>
    public class MarketingCommand
    {
        /// <summary>
        /// 限时购活动ID
        /// </summary>
        public long FlashSaleId { get; set; }

        /// <summary>
        /// 拼团活动ID
        /// </summary>
        public long GrouponId { get; set; }

        /// <summary>
        /// 拼团活动参团ID
        /// </summary>
        public long GrouponGroupId { get; set; }

        /// <summary>
        /// 组合购ID
        /// </summary>
        public long CollocationId { get; set; }

        /// <summary>
        /// 选中记录(优惠卷,平台券,红包)
        /// </summary>
        public List<GeneralRecordChoice> Choices { get; set; }
    }
}
