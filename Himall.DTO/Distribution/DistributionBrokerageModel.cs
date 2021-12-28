using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.OrderInfo;

namespace Himall.DTO
{

    public class DistributionBrokerageModel : DistributionBrokerageInfo
    {
        public DateTime FinishDate { get; set; }
        public OrderOperateStatus OrderStatus { get; set; }
    }
}
