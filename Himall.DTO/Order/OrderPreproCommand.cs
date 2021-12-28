using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 订单预提交命令
    /// </summary>
    public class OrderPreproCommand
    {
        /// <summary>
        /// 会员ID
        /// </summary>
        public long MemberId { get; set; }
        /// <summary>
        /// 收货地址
        /// </summary>
        public long AddressId { get; set; }

        public PlatformType PlatformType { get; set; }

        /// <summary>
        /// 门店地址(门店购物车提交订单)
        /// </summary>
        public long ShopBranchId { get; set; }
        /// <summary>
        /// 虚拟商品
        /// </summary>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// 购物车
        /// </summary>
        public List<long> CartItems { get; set; }

        /// <summary>
        /// 商品项目
        /// </summary>
        public List<ProductItem> Items { get; set; }

        /// <summary>
        /// 商品项目
        /// </summary>
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
            /// 数量
            /// </summary>
            public int Quantity { get; set; }
            /// <summary>
            /// 直播间ID
            /// </summary>
            public long RoomId { get; set; }
        }

        /// <summary>
        /// 组合购ID
        /// </summary>
        public long CollocationId { get; set; }

        /// <summary>
        /// 秒杀ID
        /// </summary>
        public long FlashSaleId { get; set; }
        /// <summary>
        /// 拼团活动ID
        /// </summary>
        public long GrouponId { get; set; }
        /// <summary>
        /// 拼团参团ID
        /// </summary>
        public long GroupId { get; set; }

        /// <summary>
        /// 优惠券
        /// </summary>
        public List<GeneralRecordChoice> Records { get; set; }

        /// <summary>
        /// 配送方式
        /// </summary>
        public List<DeliveryTypeChoice> DeliverTypes { get; set; }
    }

    public class DeliveryTypeChoice
    {
        public long ShopId { get; set; }
        public DeliveryType DeliveryType { get; set; }
    }

}
