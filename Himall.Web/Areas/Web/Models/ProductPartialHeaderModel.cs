using Himall.CommonModel;
using Himall.DTO;
using Himall.Entities;
using System.Collections.Generic;
namespace Himall.Web.Areas.Web.Models
{
    public class ProductPartialHeaderModel
    {
		public long ShopId { get; set; }
        public string isLogin { get; set; }
        public int MemberIntegral { get; set; }
        public List<FavoriteInfo> Concern { get; set; }
        public List<DisplayCoupon> BaseCoupon { get; set; }
        public List<ProductBrowsedHistoryModel> BrowsingProducts { get; set; }

		public List<CustomerService> PlatformCustomerServices { get; set; }
	}
}