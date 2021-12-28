using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.QueryModel
{
    public class BrandApplyQuery:QueryBase
    {
        public ShopBrandApplyInfo.BrandAuditStatus? AuditStatus { get; set; }
        public long? ShopId { get; set; }
        public string Keywords { get; set; }


    }
}
