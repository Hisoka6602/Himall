using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 直播中直播间
    /// </summary>
    public class LiveRoomData
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long RoomId { get; set; }
        public long ShopId { get; set; }
        /// <summary>
        /// 商品
        /// </summary>
        public List<LiveProductData> Products { get; set; }
    }
    public class LiveProductData {
        public long ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
