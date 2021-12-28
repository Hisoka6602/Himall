
using System;
namespace Himall.DTO
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ChargeDetailModel
    {
        public string Id { get; set; }
        public long MemId { get; set; }
        public string ChargeTime { get; set; }
        public decimal ChargeAmount { get; set; }
        public string ChargeWay { get; set; }
        public Himall.Entities.ChargeDetailInfo.ChargeDetailStatus ChargeStatus { get; set; }

        public string ChargeStatusDesc { get; set; }
        public string CreateTime { get; set; }
        public decimal PresentAmount { get; set; }
    }
}