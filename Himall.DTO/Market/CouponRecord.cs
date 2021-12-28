using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 优惠券记录
    /// </summary>
    public class CouponRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 面值
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 优惠券活动ID
        /// </summary>
        public long CouponId { get; set; }
        /// <summary>
        /// 优惠券名称
        /// </summary>
        public string CouponName { get; set; }
        /// <summary>
        /// 所属商家
        /// </summary>
        public long ShopId { get; set; }

        /// <summary>
        /// 使用门槛
        /// </summary>
        public decimal OrderAmount { get; set; }
        /// <summary>
        /// 限制使用区域
        /// </summary>
        public bool UseArea { get; set; }
    }
}
