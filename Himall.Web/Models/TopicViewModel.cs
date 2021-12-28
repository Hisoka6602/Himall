using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Models
{
    public class TopicViewModel
    {
        public TopicInfo Topic { get; set; }

        public List<TopicModuleInfo> Modules { get; set; }
        public List<ModuleProductInfo> ModuleProducts { get; set; }

        public List<ProductInfo> Products { get; set; }
    }

   
}