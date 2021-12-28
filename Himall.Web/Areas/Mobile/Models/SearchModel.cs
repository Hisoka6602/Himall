using Himall.DTO;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Web.Areas.Mobile.Models
{
    public class SearchModel
    {
        public TypeAttributesModel[] Attrs { get; set; }

        public BrandInfo[] Brands { get; set; }

        public SellerAdmin.Models.CategoryJsonModel[] Category { get; set; }

        public Dictionary<string , string> AttrDic { get; set; }

        public long cid { get; set; }

        public long b_id { get; set; }

        public string a_id { get; set; }

        public int Total { get; set; }

        public string Keywords { get; set; }
    }
}