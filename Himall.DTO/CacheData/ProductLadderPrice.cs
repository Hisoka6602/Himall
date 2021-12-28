using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class ProductLadderPriceData
    {
        /// <summary>
        /// 最小批次
        /// </summary>
        public int MinBath { get; set; }
        /// <summary>
        /// 最大批次
        /// </summary>
        public int MaxBath { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }
    }
}
