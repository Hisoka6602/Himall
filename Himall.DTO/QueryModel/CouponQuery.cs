using static Himall.Entities.CouponInfo;

namespace Himall.DTO.QueryModel
{
    public partial class CouponQuery : QueryBase
    {
        public string CouponName { get; set; }
        /// <summary>
        /// Null表示取所有
        /// </summary>
        public long? ShopId { get; set; }
        /// <summary>
        /// 显示平台
        /// </summary>
        public Himall.Core.PlatformType? ShowPlatform { get; set; }
        /// <summary>
        /// 仅显示正常
        /// </summary>
        public bool? IsOnlyShowNormal { get; set; }
        /// <summary>
        /// 是否显示所有
        /// <para>默认仅显示正常优惠券</para>
        /// </summary>
        public bool? IsShowAll { get; set; }

        /// <summary>
        /// 0全场通用,1部分商家用
        /// </summary>
        public int? UseArea { get; set; }

        /// <summary>
        /// 领取方式
        /// </summary>
        public CouponReceiveType? ReceiveType { get; set; }
    }
}
