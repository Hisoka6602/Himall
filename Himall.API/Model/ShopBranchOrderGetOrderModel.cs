using Himall.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.API.Model
{
    public class ShopBranchOrderGetOrderModel : Order
    {
        public bool CanDaDaExpress { get; set; }
    }
    public class PrintOrder
    {
        public long OrderId { get; set; }
        public int PrintCount { get; set; }
    }
}
