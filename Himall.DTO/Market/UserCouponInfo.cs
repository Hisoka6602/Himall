using Himall.Entities;
using System;
using System.Collections.Generic;

namespace Himall.DTO
{
    public class UserCouponInfo : IBaseCoupon
    {
        public long UserId { get; set; }
        public long CouponId { get; set; }
        public long ShopId { get; set; }
        public Nullable<long> VShopId { get; set; }
        public string ShopName { get; set; }
        public string ShopLogo { get; set; }
        public decimal Price { get; set; }
        public int PerMax { get; set; }
        public decimal OrderAmount { get; set; }
        public int Num { get; set; }
        public System.DateTime StartTime { get; set; }
        public System.DateTime EndTime { get; set; }
        public string CouponName { get; set; }
        public System.DateTime CreateTime { get; set; }

        public Nullable<DateTime> UseTime { get; set; }
        public Entities.CouponRecordInfo.CounponStatuses UseStatus { get; set; }
        public Nullable<long> OrderId { get; set; }

        public Entities.VShopInfo VShop { get; set; }

        public List<Entities.VShopInfo> VShops { get; set; }

        public string VshopNames { get; set; }
        public CouponType BaseType
        {
            get { return CouponType.Coupon; }
        }

        public string BaseShopName
        {
            get { return this.ShopName; }
        }



        /// <summary>
        /// 优惠券领取状态
        /// </summary>
        public int ReceiveStatus
        {
            get; set;
        }

        public string Remark { get; set; }

        /// <summary>
        /// 使用范围：0=全场通用，1=部分商品可用
        /// </summary>
        public int UseArea { get; set; }
    }
}
