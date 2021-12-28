
namespace Himall.DTO
{
    /// <summary>
    /// 会员资产信息
    /// </summary>
    public  class MemberCapital 
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public string UserCode { get; set; }

        public string UserName { get;set;}
        public string RealName { get; set; }
        public string CellPhone { get; set; }
        public string Nick { get; set; }

        public decimal Balance { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal FreezeAmount { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal PresentAmount { get; set; }


    }
}