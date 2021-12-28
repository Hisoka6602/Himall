namespace Himall.DTO.QueryModel
{
    public partial class GiftsOrderQuery : QueryBase
    {
        /// <summary>
        /// 关键词
        /// </summary>
        public string Skey { get; set; }
        /// <summary>
        /// 订单编号
        /// </summary>
        public long? OrderId { get; set; }

        public Himall.Entities.GiftOrderInfo.GiftOrderStatus? Status { get; set; }

        public long? UserId { get; set; }
    }
}
