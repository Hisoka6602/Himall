using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Himall.DTO
{
    /// <summary>
    /// 提交订单结果
    /// </summary>
    public class OrderSubmitResult
    {
        public List<long> Orders { get; set; }
        public decimal Amount { get; set; }
    }
}
