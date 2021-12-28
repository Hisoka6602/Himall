using Himall.CommonModel;
using System;

namespace Himall.DTO.QueryModel
{
    public class FullDiscountActiveQuery : QueryBase
    {
        public long? ShopId { set; get; }
        public FullDiscountStatus? Status { set; get; }
        public DateTime? StartTime { set; get; }
        public DateTime? EndTime { set; get; }
        public string ActiveName { set; get; }
    }
}
