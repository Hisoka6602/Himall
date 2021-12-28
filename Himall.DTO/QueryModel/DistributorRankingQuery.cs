using Himall.DTO.QueryModel;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class DistributorRankingQuery : QueryBase
    {
        /// <summary>
        /// 批次(必填参数)
        /// </summary>
        public long BatchId { get; set; }
    }
}
