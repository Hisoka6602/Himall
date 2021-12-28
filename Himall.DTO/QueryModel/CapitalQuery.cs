namespace Himall.DTO.QueryModel
{
    public class CapitalQuery : QueryBase
    {
        public long MemberId { get; set; }
        public string UserName { get; set; }
        public string CellPhone { get; set; }
        public string Nick { get; set; }
        public string RealName { get; set; }

    }
}
