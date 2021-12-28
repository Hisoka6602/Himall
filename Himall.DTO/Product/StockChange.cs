using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Product
{
    public class StockChange
    {
        public long ShopBranchId { get; set; }
        public long ProductId { get; set; }
        public string SkuId { get; set;}
        public int Number { get; set; }
    }
}
