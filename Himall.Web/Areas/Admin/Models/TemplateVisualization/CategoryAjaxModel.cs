using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    /// <summary>
    /// 商品类型实体
    /// </summary>
    public class CategoryAjaxModel
    {
        public int status { get; set; }
        public List<CategorysContent> list { get; set; }
    }

    public class CategorysContent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string pc_link { get; set; }
    }
}