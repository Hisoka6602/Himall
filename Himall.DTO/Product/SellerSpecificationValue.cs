using Himall.CommonModel;

namespace Himall.DTO
{
    public class SellerSpecificationValue
	{
		public long Id { get; set; }
		public long ShopId { get; set; }
		public long ValueId { get; set; }
		public SpecificationType Specification { get; set; }
		public long TypeId { get; set; }
		public string Value { get; set; }
	}
}
