using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 预处理优惠券结果
    /// </summary>
    public class PreproRecordResult
    {
        /// <summary>
        /// 优惠券ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 优惠券类型
        /// </summary>
        public CouponType Type { get; set; }
        /// <summary>
        /// 所属商家
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 优惠券面值
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// 实际抵扣金额
        /// </summary>
        public decimal ActualAmount { get; set; }
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool Selected { get; set; }
        /// <summary>
        /// 关联项目
        /// </summary>
        [JsonIgnore]
        public List<MarketingOrderItem> Items { get; set; }

        /// <summary>
        /// 优惠券类型
        /// </summary>
        public enum CouponType
        {
            /// <summary>
            /// 优惠券
            /// </summary>
            Coupon = 1,
            /// <summary>
            /// 平台券
            /// </summary>
            Platform = 2,
            /// <summary>
            /// 代金红包
            /// </summary>
            Bouns = 3,
        }
    }
}
