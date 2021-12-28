using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Product
{
    /// <summary>
    /// 运费计算项目
    /// </summary>
    public class FreightItem
    {
        public long FreightTemplateId { get; set; }
        public decimal Weight { get; set; }
        public decimal Volume { get; set; }
        public decimal Amount { get; set; }
        public int Quantity { get; set; }

        public long ProductId { get; set; }

    }
}
