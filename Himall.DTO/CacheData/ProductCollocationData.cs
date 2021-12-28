using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 组合购
    /// </summary>
    public class CollocationData
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long ShopId { get; set; }
        public long ProductId { get; set; }

        public List<CollocationProductData> Products { get; set; }
    }
    public class CollocationProductData
    {
        public long ProductId { get; set; }
        public bool IsMain { get; set; }
        public long ColloId { get; set; }

        public long ColloProId { get; set; }
        public int DisplaySequence { get; set; }
        
        
        public string SkuId { get; set; }
        public decimal Price { get; set; }
        public decimal SkuPirce { get; set; }
    }
}
