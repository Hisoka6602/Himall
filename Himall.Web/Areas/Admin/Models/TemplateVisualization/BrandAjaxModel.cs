using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    public class BrandAjaxModel
    {
        public string page { get; set; }
        public List<BrandsContent> list { get; set; }
        public int status { get; set; }
    }

    public class BrandsContent
    {
        public long Id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string pc_link { get; set; }
        public string pic { get; set; }
    }
}