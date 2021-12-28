using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.CacheData;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.OrderInfo;

namespace Himall.DTO
{
    /// <summary>
    /// 订单创建模型
    /// </summary>
    public class OrderCreating
    {
        public MemberData Member { get; set; }
        public PlatformType Platform { get; set; }
        /// <summary>
        /// 收货地址
        /// </summary>
        public ShippingAddressData Address { get; set; }
        public long RoomId { get; set; }
        public List<SubOrder> SubOrders { get; set; }
        /// <summary>
        /// 是否扣减库存
        /// </summary>
        public bool IsDecreaseStock { get; set; }
        /// <summary>
        /// 商家子订单
        /// </summary>
        public class SubOrder
        {
            /// <summary>
            /// 订单号
            /// </summary>
            public long OrderId { get; set; }
            /// <summary>
            /// 所属商家
            /// </summary>
            public long ShopId { get; set; }
            /// <summary>
            /// 商家名称
            /// </summary>
            public string ShopName { get; set; }
            /// <summary>
            /// 所属门店
            /// </summary>
            public long ShopBranchId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public OrderTypes OrderType { get; set; }
            /// <summary>
            /// 配送方式
            /// </summary>
            public DeliveryType DeliveryType { get; set; }
            /// <summary>
            /// 备注
            /// </summary>
            public string Remark { get; set; }
            /// <summary>
            /// 虚拟订单
            /// </summary>
            public bool IsVirtual { get; set; }
            /// <summary>
            /// 商品项目
            /// </summary>
            public List<ProductItem> Items { get; set; }
            /// <summary>
            /// 使用预存款
            /// </summary>
            public decimal Capital { get; set; }
            /// <summary>
            /// 积分抵扣金额
            /// </summary>
            public decimal IntegralDiscount { get; set; }
            /// <summary>
            /// 营销活动包邮标记
            /// </summary>
            public bool FreeFreight { get; set; }
            public decimal Freight { get; set; }
            /// <summary>
            /// 税费
            /// </summary>
            public decimal Tax { get; set; }
            /// <summary>
            /// 订单商品金额(优惠与积分抵扣)
            /// </summary>
            public decimal ProductAmount => Items.Sum(p => p.Amount);
            /// <summary>
            /// 订单金额(商品金额,邮费,税费)
            /// </summary>
            public decimal OrderAmount => ProductAmount + Tax + Freight - IntegralDiscount;
            /// <summary>
            /// 发票信息
            /// </summary>
            public OrderInvoice Invoice { get; set; }
            /// <summary>
            /// 商家优惠卷
            /// </summary>
            public long CouponId { get; set; }
            /// <summary>
            ///平台优惠卷活动ID
            /// </summary>
            public long PlatformCouponId { get; set; }
            /// <summary>
            /// 货到付款
            /// </summary>
            public bool IsCashOnDelivery { get; set; }
            public long BonusId { get; set; }
        }

        /// <summary>
        /// 商品项目
        /// </summary>
        public class ProductItem
        {
            /// <summary>
            /// 商品
            /// </summary>
            public long ProductId { get; set; }

            public string ProductName { get; set; }
            public string Color { get; set; }
            public string Size { get; set; }
            public string Version { get; set; }

            public string Thumbnails { get; set; }

            /// <summary>
            /// SKUID
            /// </summary>
            public string SkuId { get; set; }
            public string SKU { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SalePrice { get; set; }
            public int Quantity { get; set; }
            /// <summary>
            /// 总优惠金额
            /// </summary>
            public decimal Discount { get; set; }
            /// <summary>
            /// 满减优惠
            /// </summary>
            public decimal FullDiscount { get; set; }
            /// <summary>
            /// 商家优惠
            /// </summary>
            public decimal CouponDiscount { get; set; }
            /// <summary>
            /// 平台优惠
            /// </summary>
            public decimal PlatformDiscount { get; set; }

            /// <summary>
            /// 虚拟商品信息
            /// </summary>
            public List<VirtualContent> VirtualItems { get; set; }
            /// <summary>
            /// 限时购ID
            /// </summary>
            public long FlashSaleId { get; set; }
            /// <summary>
            /// 小计
            /// </summary>
            public decimal Amount { get; set; }
            /// <summary>
            /// 平台佣金
            /// </summary>
            public decimal CommisRate { get; set; }
            /// <summary>
            /// 直播间ID
            /// </summary>

            public long RoomId { get; set; }

        }

        /// <summary>
        /// 虚拟商品信息
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
    /// <summary>
    /// 发票信息
    /// </summary>
    public class OrderInvoice
    {
        /// <summary>
        /// 发票类型
        /// </summary>
        public InvoiceType InvoiceType { get; set; }
        /// <summary>
        /// 发票抬头
        /// </summary>
        public string InvoiceTitle { get; set; }
        /// <summary>
        /// 税号
        /// </summary>
        public string InvoiceCode { get; set; }
        /// <summary>
        /// 发票内容
        /// </summary>
        public string InvoiceContext { get; set; }
        /// <summary>
        /// 注册地址
        /// </summary>
        public string RegisterAddress { get; set; }
        /// <summary>
        /// 注册电话
        /// </summary>
        public string RegisterPhone { get; set; }
        /// <summary>
        /// 开户行
        /// </summary>
        public string BankName { get; set; }
        /// <summary>
        /// 银行账号
        /// </summary>
        public string BankNo { get; set; }
        /// <summary>
        /// 收票人
        /// </summary>
        public string RealName { get; set; }
        /// <summary>
        /// 收票联系电话
        /// </summary>
        public string CellPhone { get; set; }
        /// <summary>
        /// 收票邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 收票地区ID
        /// </summary>
        public string RegionID { get; set; }
        /// <summary>
        /// 收票地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 开票时间
        /// </summary>
        public int VatInvoiceDay { get; set; }
    }
}
