using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    /// <summary>
    /// 店铺实体
    /// </summary>
    public class ShopsAjaxModel
    {
        public int status { get; set; }
        public List<ShopsContent> list { get; set; }
        public string page { get; set; }
    }

    public class ShopsContent
    {
        public long shopId { get; set; }
        public string shopGrade { get; set; }
        public string shopName { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string pc_link { get; set; }
    }
}