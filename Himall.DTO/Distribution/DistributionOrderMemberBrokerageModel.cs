using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class DistributionOrderMemberBrokerageModel
    {
        public long OrderId { get; set; }
        public long MemberId { get; set; }
        public decimal Brokerage { get; set; }
        public DateTime SettledDate { get; set; }
    }
}
