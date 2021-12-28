using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 门店
    /// </summary>
    public class ShopBranchData
    {
        public long Id { get; set; }
        public long ShopId { get; set; }
        public string ShopBranchName { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        /// <summary>
        /// 服务半径
        /// </summary>
        public int ServeRadius { get; set; }
        /// <summary>
        /// 支持门店配送
        /// </summary>
        public bool IsStoreDelive { get; set; }
        /// <summary>
        /// 支持自提
        /// </summary>
        public bool IsAboveSelf { get; set; }
        /// <summary>
        /// 登录账号
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 详细地址
        /// </summary>
        public string AddressDetail { get; set; }
        /// <summary>
        /// 联系电话
        /// </summary>
        public string ContactPhone { get; set; }
        /// <summary>
        /// 行政区域ID
        /// </summary>
        public int AddressId { get; set; }
        /// <summary>
        /// 配送费
        /// </summary>
        public decimal DeliveFee { get; set; }
        /// <summary>
        /// 包邮金额
        /// </summary>
        public decimal FreeMailFee { get; set; }
        /// <summary>
        /// 是否包邮
        /// </summary>
        public bool IsFreeMail { get; set; }
        /// <summary>
        /// 起送费
        /// </summary>
        public decimal DeliveTotalFee { get; set; }
    }
}
