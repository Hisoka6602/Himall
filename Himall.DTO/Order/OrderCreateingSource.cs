using Himall.DTO.CacheData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 订单创建数据源
    /// </summary>
    public class OrderCreateingSource
    {
        /// <summary>
        /// 收货地址
        /// </summary>
        public ShippingAddressData Address { get; set; }
        /// <summary>
        /// 商品相关信息
        /// </summary>
        public List<ProductData> Products { get; set; }

        /// <summary>
        /// 商家信息
        /// </summary>
        public List<ShopData> Shops { get; set; }
        public MemberData Member { get; set; }
    }
}
