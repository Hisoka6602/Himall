using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.Market;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.DTO
{
    /// <summary>
    /// 订单提交命令
    /// </summary>
    public class OrderCreateCommand
    {
        /// <summary>
        /// 会员ID
        /// </summary>
        public long MemberId { get; set; }
        /// <summary>
        /// 收货地址
        /// </summary>
        public long AddressId { get; set; }
        /// <summary>
        /// 使用积分
        /// </summary>
        public int UseIntegral { get; set; }
        /// <summary>
        /// 使用预存款
        /// </summary>
        public decimal UseCapital { get; set; }
        /// <summary>
        /// 支付密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 虚拟订单
        /// </summary>
        public bool IsVirtual { get; set; }
        /// <summary>
        /// 组合购
        /// </summary>
        public long CollocationId { get; set; }
        /// <summary>
        /// 秒杀
        /// </summary>
        public int FlashSaleId { get; set; }
        /// <summary>
        /// 拼团参团ID
        /// </summary>
        public int GrouponGroupId { get; set; }
        /// <summary>
        /// 拼团活动ID
        /// </summary>
        public int GrouponId { get; set; }
        /// <summary>
        /// 购物车
        /// </summary>
        public List<long> CartItems { get; set; }
        /// <summary>
        /// 优惠券
        /// </summary>
        public List<GeneralRecordChoice> Choices { get; set; }

        /// <summary>
        /// 商家分组
        /// </summary>
        public List<SubOrder> Subs { get; set; }
        /// <summary>
        /// 直播间
        /// </summary>
        public long RoomId { get; set; }
        public PlatformType PlatformType { get; set; }

        /// <summary>
        /// 页面计算价格
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 商家分组
        /// </summary>
        public class SubOrder
        {
            /// <summary>
            /// 商家
            /// </summary>
            public long ShopId { get; set; }
            /// <summary>
            /// 门店
            /// </summary>
            public long ShopBranchId { get; set; }
            /// <summary>
            /// 配送方式
            /// </summary>
            public DeliveryType DeliveryType { get; set; }
            /// <summary>
            /// 货到付款
            /// </summary>
            public bool IsCashOnDelivery { get; }
            public PaymentType PaymentType { get; set; }
            /// <summary>
            /// 发票信息
            /// </summary>
            public Invoices Invoice { get; set; }
            /// <summary>
            /// 商品项目
            /// </summary>
            public List<ProductItem> Items { get; set; }
            public string Remark { get; set; }
        }

        /// <summary>
        /// 商品项目
        /// </summary>
        public class ProductItem
        {
            public long RoomId { get; set; }
            public long ProductId { get; set; }
            public string SkuId { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            /// <summary>
            /// 满额减
            /// </summary>
            public decimal FullDiscount { get; set; }
            /// <summary>
            /// 优惠券抵扣
            /// </summary>
            public decimal CouponDiscount { get; set; }
            /// <summary>
            /// 积分抵扣
            /// </summary>
            public decimal IntegralDiscount { get; set; }
            /// <summary>
            /// 平台券抵扣
            /// </summary>
            public decimal PlatformDicount { get; set; }

            public decimal Amount { get; set; }
            /// <summary>
            /// 虚拟商品附加属性
            /// </summary>
            public List<VirtualContent> VirtualContents { get; set; }

        }

        /// <summary>
        /// 虚拟商品补充信息
        /// </summary>
        public class VirtualContent
        {
            /// <summary>
            /// 标题
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 内容
            /// </summary>
            public string Content { get; set; }
            /// <summary>
            /// 内容类型
            /// </summary>
            public ProductInfo.VirtualProductItemType Type { get; set; }
        }
    }
}
