using Himall.Core;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductInfo;

namespace Himall.DTO.Live
{
    public class LiveProductLibaryModel : LiveProductLibraryInfo
    {
        /// <summary>
        /// 商品名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 图片
        /// </summary>
        public string Image { get; set; }
        /// <summary>
        /// 价格类型
        /// </summary>
        public PriceTypes PriceType { get { return MinSalePrice == MaxSalePrice ? (MarketPrice.HasValue && MarketPrice.Value > 0 && MarketPrice.Value < MinSalePrice ? PriceTypes.DiscountPrice : PriceTypes.Price) : PriceTypes.RangPrice; } }
        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get { return MinSalePrice; } }
        /// <summary>
        /// 区间价/折扣价
        /// </summary>
        public decimal Price2 { get { return MinSalePrice == MaxSalePrice ? (MarketPrice.HasValue && MarketPrice.Value > 0 && MarketPrice.Value < MinSalePrice ? MarketPrice.Value : 0) : MaxSalePrice; } }
        /// <summary>
        /// 市场价
        /// </summary>
        public decimal? MarketPrice { get; set; }
        /// <summary>
        /// 最小销售价
        /// </summary>
        public decimal MinSalePrice { get; set; }
        /// <summary>
        /// 最大销售价
        /// </summary>
        public decimal MaxSalePrice { get; set; }
        /// <summary>
        /// 店铺名称
        /// </summary>
        public string ShopName { get; set; }

        public string AuditStatusStr { get { return LiveAuditStatus.ToDescription(); } }

    }
}
