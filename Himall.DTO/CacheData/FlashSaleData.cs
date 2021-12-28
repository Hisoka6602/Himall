using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class FlashSaleData
    {
        public long Id { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 活动名称
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime BeginDate { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndDate { get; set; }
        /// <summary>
        /// 项目
        /// </summary>
        public List<FlashSaleItemData> Items { get; set; }
        public int LimitCountOfThePeople { get; set; }
        public FlashSaleInfo.FlashSaleStatus Status { get; set; }
    }

    public class FlashSaleItemData 
    { 
        public string SkuId { get; set; }
        public decimal Price { get; set; }
    }

    public class FlashSaleItemSimp { 
        public string SkuId { get; set; }

        /// <summary>
        /// 活动数量
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 商品库存
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// 剩余数量
        /// </summary>
        public int Surplus => Math.Min(Number, Stock);
        /// <summary>
        /// 活动价格
        /// </summary>
        public decimal Price { get; set; }
    }
}
