using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.SmallProgAPI.Model
{
    public class OrderCommentModel
    {
        public long OrderId { get; set; }

        public IEnumerable<ProductCommentModel> ProductComments { get; set; }

        public int Score { get; set; }

        public int PackMark { get; set; }
        public int DeliveryMark { get; set; }
        public int ServiceMark { get; set; }

    }
    public class ProductCommentModel
    {
        public long ProductId { get; set; }

        public string[] Images { set; get; }

        public string[] WXmediaId { set; get; }

        public int Mark { set; get; }

        public string Content { get; set; }

        public long OrderItemId { get; set; }
    }
}
