using Himall.CommonModel;

namespace Himall.DTO
{
    public class SpecificationValue
	{
		public new long Id { get; set; }
		public SpecificationType Specification { get; set; }
		public long TypeId { get; set; }
		public string Value { get; set; }
	}
}
