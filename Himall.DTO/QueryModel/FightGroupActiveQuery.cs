using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.ProductInfo;

namespace Himall.DTO.QueryModel
{
    public class FightGroupActiveQuery : QueryBase
    {
        public string ProductName { get; set; }
        public FightGroupActiveStatus? ActiveStatus { get; set; }
        public List<FightGroupActiveStatus> ActiveStatusList { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ShopName { get; set; }
        public long? ShopId { get; set; }

        /// <summary>
        /// 产品销售状态
        /// </summary>
        public ProductSaleStatus? SaleStatus { get; set; }

        /// <summary>
        /// 分类Id
        /// </summary>
        public long CategoryId { get; set; }

        /// <summary>
        /// 多个分类
        /// </summary>

        public List<long> Categories { get; set; }
    }
}
