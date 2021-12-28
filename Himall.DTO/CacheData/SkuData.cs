using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class SkuData
    {
        /// <summary>
        /// 商品ID_颜色规格ID_颜色规格ID_尺寸规格
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 自增主键Id
        /// </summary>
        public long AutoId { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// 颜色规格
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// 尺寸规格
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 版本规格
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// SKU
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// 成本价
        /// </summary>
        public decimal CostPrice { get; set; }

        /// <summary>
        /// 销售价
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// 显示图片
        /// </summary>
        public string ShowPic { get; set; }

        /// <summary>
        /// 警戒库存
        /// </summary>
        public long SafeStock { get; set; }

        /// <summary>
        /// 是否旺店通推送
        /// </summary>
        public bool PushWdtState { get; set; }

        /// <summary>
        /// 颜色别名
        /// </summary>
        public string ColorAlias { get; set; }

        /// <summary>
        /// 尺寸别名
        /// </summary>
        public string SizeAlias { get; set; }

        /// <summary>
        /// 规格别名
        /// </summary>
        public string VersionAlias { get; set; }
    }
}
