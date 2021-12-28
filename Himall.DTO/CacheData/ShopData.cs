using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class ShopData
    {
        /// <summary>
        /// 标识ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 商家名称
        /// </summary>
        public string ShopName { get; set; }
        /// <summary>
        /// 免邮门槛
        /// </summary>
        public decimal FreeFreight { get; set; }
        /// <summary>
        /// 自动分配订单
        /// </summary>
        public bool AutoAllotOrder { get; set; }
    }
   

}
