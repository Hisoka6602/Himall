using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    /// <summary>
    /// 共用实体(门店标签等可以使用)
    /// </summary>
    public class CommonAjaxModel
    {
        public int status { get; set; }
        public List<CommonContent> list { get; set; }
        public string page { get; set; }
    }

    public class CommonContent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string wap_link { get; set; }
        public string pc_link { get; set; }
    }
}