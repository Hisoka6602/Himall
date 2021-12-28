using Himall.CommonModel;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.OrderInfo;

namespace Himall.DTO.QueryModel
{
    public class DistributionBrokerageQuery : QueryBase
    {
        /// <summary>
        /// 分销员ID
        /// </summary>
        public long DistributorId { get; set; }
    }
}
