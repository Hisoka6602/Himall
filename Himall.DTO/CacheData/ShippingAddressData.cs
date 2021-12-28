using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 收货地址
    /// </summary>
    public class ShippingAddressData
    {
        public long Id { get; set; }
        public string ShipTo { get; set; }
        public string Phone { get; set; }

        public int RegionId { get; set; }

        public string RegionFullName { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 详细地址
        /// </summary>
        public string AddressDetail { get; set; }
        /// <summary>
        /// 默认地址
        /// </summary>
        public bool IsDefault { get; set; }
        /// <summary>
        /// 轻松购
        /// </summary>
        public bool IsQuick { get; set; }
        public decimal Longitude{ get; set; }
        
        public decimal Latitude { get; set; }
    }
}
