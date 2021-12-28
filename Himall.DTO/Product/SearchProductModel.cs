using Himall.Entities;
using System.Collections.Generic;

namespace Himall.DTO
{

    public class SearchProductModel
    {
        public List<BrandInfo> Brands { get; set; }

        public List<TypeAttributesModel> ProductAttrs { get; set; }
    }
}
