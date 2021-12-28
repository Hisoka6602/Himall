using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductInfo;

namespace Himall.DTO.Live
{
    public class LiveProduct
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
        public long ProductId { get; set; }
        public decimal Price { get; set; }

        public int SaleCount { get; set; }

        public decimal SaleAmount { get; set; }

        /// <summary>
        /// 价格2
        /// </summary>
        public decimal Price2
        {
            get;set;
        }




        /// <summary>
        /// 价格类型
        /// </summary>
        public PriceTypes PriceType
        {
            get;set;
        }
    }
}
