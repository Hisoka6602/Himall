using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{
    [Flags]
    public enum MarketingType
    {
        /// <summary>
        /// 代金红包
        /// </summary>
        Bonus = 1,
        /// <summary>
        /// 优惠券
        /// </summary>
        Coupon = 2,
        /// <summary>
        /// 限时购
        /// </summary>
        FlashSale = 4,
        /// <summary>
        /// 拼团
        /// </summary>
        Groupon = 8,
        /// <summary>
        /// 满额减
        /// </summary>
        FullDiscount = 16,
        /// <summary>
        /// 组合购
        /// </summary>
        Collocation = 32,
        /// <summary>
        /// 会员折扣
        /// </summary>
        MemberDiscount = 64,
        /// <summary>
        /// 平台券
        /// </summary>
        PlatformCoupon = 128,
    }
}
