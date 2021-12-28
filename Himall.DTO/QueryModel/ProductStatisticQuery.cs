using System;

namespace Himall.DTO.QueryModel
{
    public class ProductStatisticQuery : QueryBase
    {
        public long? ShopId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
