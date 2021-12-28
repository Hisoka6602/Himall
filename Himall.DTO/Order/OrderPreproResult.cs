using Himall.CommonModel;
using Himall.DTO.CacheData;
using Himall.DTO.Market;
using Himall.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 订单预提交结果
    /// </summary>
    public class OrderPreproResult
    {
        /// <summary>
        /// 会员
        /// </summary>
        public long MemberId { get; set; }
        /// <summary>
        /// 可用积分
        /// </summary>
        public int Integral { get; set; }
        /// <summary>
        /// 积分抵扣规则
        /// </summary>
        public int IntegralPerMoney { get; set; }
        /// <summary>
        /// 积分最大抵扣金额
        /// </summary>
        public decimal IntegralMaxMoney { get; set; }
        /// <summary>
        /// 收货地址
        /// </summary>
        public ShippingAddressData Address { get; set; }
        /// <summary>
        /// 核销地址
        /// </summary>
        public PickupAddressData PickupAddress { get; set; }
        /// <summary>
        /// 核销地址
        /// </summary>
        public class PickupAddressData
        {
            /// <summary>
            /// 详细地址
            /// </summary>
            public string Address { get; set; }
            /// <summary>
            /// 联系电话
            /// </summary>
            public string Contact { get; set; }
        }

        public List<string> InvoiceContext { get; set; }
        /// <summary>
        /// 是否含有支付密码
        /// </summary>
        public bool IsPassword { get; set; }
        /// <summary>
        /// 预存款余额
        /// </summary>
        public decimal CapitalAmount { get; set; }
        /// <summary>
        /// 子订单
        /// </summary>
        public List<SubOrder> SubOrders { get; set; }

        /// <summary>
        /// 可用平台券
        /// </summary>
        public List<GeneralRecord> Records { get; set; }

        /// <summary>
        /// 合计金额
        /// </summary>
        public decimal Amount => SubOrders.Sum(p => p.Amount);
        public class SubOrder
        {
            /// <summary>
            /// 商家ID
            /// </summary>
            public long ShopId { get; set; }
            /// <summary>
            /// 商家名称
            /// </summary>
            public string ShopName { get; set; }

            /// <summary>
            /// 门店ID
            /// </summary>
            public long ShopBranchId { get; set; }

            /// <summary>
            /// 门店名称
            /// </summary>
            public string ShopBranchName { get; set; }
            /// <summary>
            /// 门店附近信息
            /// </summary>
            public ShopBranchAttach ShopBranchAttach { get; set; }
            /// <summary>
            /// 配送方式
            /// </summary>
            public DeliveryType DeliveryType { get; set; }
            /// <summary>
            /// 是否支持货到付款
            /// </summary>
            public bool IsCashOnDelivery { get; set; }
            /// <summary>
            /// 商品信息
            /// </summary>
            public List<ProductItem> Items { get; set; }
            /// <summary>
            /// 可用(优惠券,代金红包)
            /// </summary>
            public List<GeneralRecord> Records { get; set; }
            /// <summary>
            /// 门店配送
            /// </summary>
            public bool IsStoreDelive { get; set; }
            /// <summary>
            /// 门店自提
            /// </summary>
            public bool IsPickup { get; set; }
            /// <summary>
            /// 运费
            /// </summary>
            public decimal Freight { get; set; }
            /// <summary>
            /// 免邮
            /// </summary>
            public bool FreeFreight { get; set; }
            /// <summary>
            /// 免邮
            /// </summary>
            public decimal FreeFreightAmount { get; set; }
            /// <summary>
            /// 是否提供发票
            /// </summary>
            public bool IsInvoice { get; set; }

            /// <summary>
            /// 满减金额
            /// </summary>
            public decimal FullDiscount { get; set; }

            /// <summary>
            /// 商品金额小计
            /// </summary>
            public decimal Amount => Items.Sum(p => p.Amount) + Freight;

            public InvoiceConfig Invoice { get; set; }

            /// <summary>
            /// 异常信息
            /// </summary>
            public string Exception { get; set; }
        }

        public class InvoiceConfig
        {
            /// <summary>
            /// 开发票
            /// </summary>
            public bool IsInvoice { get; set; }
            /// <summary>
            /// 开普通发票
            /// </summary>
            public bool IsPlainInvoice { get; set; }
            /// <summary>
            /// 发票税率
            /// </summary>
            public decimal PlainInvoiceRate { get; set; }
            /// <summary>
            /// 开电子发票
            /// </summary>
            public bool IsElectronicInvoice { get; set; }
            /// <summary>
            /// 开增值税发票
            /// </summary>
            public bool IsVatInvoice { get; set; }
            /// <summary>
            /// 增值税率
            /// </summary>
            public decimal VatInvoiceRate { get; set; }
            /// <summary>
            /// 开票时间
            /// </summary>
            public string VatInvoiceDay { get; set; }
        }

        public class ProductItem
        {
            /// <summary>
            /// 商品ID
            /// </summary>
            public long ProductId { get; set; }
            /// <summary>
            /// SKUID
            /// </summary>
            public string SkuId { get; set; }
            /// <summary>
            /// 商家ID
            /// </summary>
            [JsonIgnore]
            public long ShopId { get; set; }

            /// <summary>
            /// 运费模板
            /// </summary>
            [JsonIgnore]
            public long FreightTemplateId { get; set; }
            /// <summary>
            /// 重量
            /// </summary>
            [JsonIgnore]
            public decimal Weight { get; set; }
            /// <summary>
            /// 体积
            /// </summary>
            [JsonIgnore]
            public decimal Volume { get; set; }
            /// <summary>
            /// 商品名
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 缩略图
            /// </summary>
            public string Thumbnail { get; set; }
            public string Color { get; set; }
            public string ColorAlias { get; set; }
            public string Size { get; set; }
            public string SizeAlias { get; set; }
            public string Version { get; set; }
            public string VersionAlias { get; set; }

            public List<VirtualItem> VirtualItems { get; set; }
            /// <summary>
            /// 数量
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// 商品单价
            /// </summary>
            public decimal Price { get; set; }

            /// <summary>
            /// 商品小计(扣除优惠)
            /// </summary>
            public decimal Amount { get; set; }
            /// <summary>
            /// 异常消息
            /// </summary>
            public string Exception { get; set; }
           
            /// <summary>
            /// 商品货号
            /// </summary>
            public string ProductCode { get; set; }
 			/// <summary>
            /// 直播间ID
            /// </summary>
            public long RoomId { get; set; }

        }

        public class ShopBranchAttach
        {
            public string Contact { get; set; }
            /// <summary>
            /// 门店地址
            /// </summary>
            public string Address { get; set; }
            /// <summary>
            /// 开店时间
            /// </summary>
            public string OpenTime { get; set; }
            /// <summary>
            /// 闭店时间
            /// </summary>
            public string CloseTime { get; set; }
        }
        /// <summary>
        /// 虚拟商品项目
        /// </summary>
        public class VirtualItem
        {
            public string Name { get; set; }
            public ProductInfo.VirtualProductItemType Type { get; set; }
            public bool Required { get; set; }
            public long Id { get; set; }
        }
    }
}
