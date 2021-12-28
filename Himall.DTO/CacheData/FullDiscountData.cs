using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 满减活动
    /// </summary>
    public class FullDiscountData
    {
        public long Id { get; set; }
        public long ShopId { get; set; }
        public string ActiveName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllProduct { get; set; }
        public List<long> Products { get; set; }

        public List<FullDiscountRuleData> Rules { get; set; }
    }
    public class FullDiscountRuleData { 
        public decimal Quota { get; set; }
        public decimal Discount { get; set; }
    }
}
