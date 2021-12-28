using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 营销订单
    /// </summary>
    public class MarketingOrder
    {
        /// <summary>
        /// 会员
        /// </summary>
        public long MemberId { get; set; }

        /// <summary>
        /// 子订单
        /// </summary>
        public List<MarketingSubOrder> SubOrders { get; set; }

    }

    /// <summary>
    /// 营销子订单模型(商家订单)
    /// </summary>
    public class MarketingSubOrder
    {
        /// <summary>
        /// 商家
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 是否免邮
        /// </summary>
        public bool FreeShipping { get; set; }
        /// <summary>
        /// 满多少免邮
        /// </summary>
        public decimal FreeFreight { get; set; }
        /// <summary>
        /// 参与活动类型
        /// </summary>
        public MarketingType MarketingTypes { get; set; }
        /// <summary>
        /// 订单项目
        /// </summary>
        public List<MarketingOrderItem> Items { get; set; }
        /// <summary>
        /// 营销项目
        /// </summary>
        public List<MarketingItem> Marketings { get; set; }
        /// <summary>
        /// 小计
        /// </summary>
        public decimal Amount => Items.Sum(p => p.Amount);
    }
    /// <summary>
    /// 营销项目
    /// </summary>
    public class MarketingItem
    {
        /// <summary>
        /// 营销活动ID
        /// </summary>
        public long MarketId { get; set; }
        /// <summary>
        /// 营销活动类型
        /// </summary>
        public MarketingType MarketingType { get; set; }
        /// <summary>
        /// 活动标品
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 优惠金额
        /// </summary>
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// 营销订单项模型
    /// </summary>
    public class MarketingOrderItem
    {
        /// <summary>
        /// 商家
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 商品SKUID
        /// </summary>
        public string SkuId { get; set; }
        /// <summary>
        /// 商品单价
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// 优惠
        /// </summary>
        public decimal Discount { get; set; }
        /// <summary>
        /// 小计
        /// </summary>
        public decimal Amount => Price * Quantity - Discount;
        /// <summary>
        /// 参与活动类型
        /// </summary>
        public MarketingType MarketingTypes { get; set; }
        /// <summary>
        /// 优惠明细
        /// </summary>
        public List<MarketingOrderItemDiscount> DiscountDetails { get; set; }
        public string ExceptionMessage { get; set; }
    }
    /// <summary>
    /// 优惠明细
    /// </summary>
    public class MarketingOrderItemDiscount
    {

        public long MarketItem { get; set; }
        public MarketingType MarketingType { get; set; }
        public decimal Amount { get; set; }
    }
}
