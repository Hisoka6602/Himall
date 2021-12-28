using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Entities;

namespace Himall.DTO.QueryModel
{
    public partial class BusinessCategoryQuery : QueryBase
    {
        public long ShopId { get; set; }
        public string CategoryName { get; set; }
    }
}

