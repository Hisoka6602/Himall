using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 优惠劵缓存数据
    /// </summary>
    public class CouponData
    {
        public long Id { get; set; }
        public long ShopId { get; set; }
        public string ShopName { get; set; }
        public decimal Price { get; set; }
        public int PerMax { get; set; }
        public decimal OrderAmount { get; set; }
        public int Num { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string CouponName { get; set; }
        public bool ShowPC { get; set; }
        public bool ShowWap { get; set; }
        public int UseArea { get; set; }

        /// <summary>
        /// 商品可用
        /// </summary>
        public List<long> Products { get; set; }
        /// <summary>
        /// 商家可用
        /// </summary>
        public List<long> Shops { get; set; }
      
        public int ReceiveType { get; set; }
        public int NeedIntegral { get; set; }
        public string Remark { get; set; }
        
    }
}
