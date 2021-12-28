using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 虚拟商品信息
    /// </summary>
    public class ProductVirtualData
    {
        public bool ValidityType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public sbyte EffectiveType { get; set; }
        public int Hour { get; set; }

        public sbyte SupportRefundType { get; set; }
        /// <summary>
        /// 使用须知
        /// </summary>
        public string UseNotice { get; set; }
        /// <summary>
        /// 虚拟项目
        /// </summary>
        public List<ProductVirtualItemData> Items { get; set; }
    }

    /// <summary>
    /// 虚拟商品自定义项目
    /// </summary>
    public class ProductVirtualItemData {
        public long Id { get; set; }
        public string Name { get; set; }
        public ProductInfo.VirtualProductItemType Type { get; set; }
        public bool Required { get; set; }
        
    }
}
