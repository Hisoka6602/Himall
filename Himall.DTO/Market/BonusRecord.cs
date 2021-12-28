using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Market
{
    /// <summary>
    /// 代金红包记录
    /// </summary>
    public class BonusRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 商家
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 红包名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 活动ID
        /// </summary>
        public long BonusId { get; set; }
        /// <summary>
        /// 红包面值
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 使用门槛
        /// </summary>
        public decimal OrderAmount { get; set; }

    }
}
