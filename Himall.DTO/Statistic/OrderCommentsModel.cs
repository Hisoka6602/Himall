using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class OrderCommentsModel
    {
        public double AvgPackMark { get; set; }
        public double AvgDeliveryMark { get; set; }

        public double AvgServiceMark { get; set; }

        public long ShopId { get; set; }

        public IList<long> CategoryIds { get; set; }
    }
}
