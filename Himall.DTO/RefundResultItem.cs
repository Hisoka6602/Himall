using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class RefundResultItem
    {
        public long OrderId
        {
            get;set;
        }
        public long UserId {
            get;set;
        }
        public long RefundId {
            get;set;
        }

        public long OrderItemId {
            get;set;
        }
    }
}
