using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class OrderUser 
    {
        /// <summary>
        /// 会员ID
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime PayDate { get; set; }
        /// <summary>
        /// 订单数
        /// </summary>
        public int OrderNumber { get; set; }
    }
}
