using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Himall.Entities;

namespace Himall.Web.Areas.Mobile.Models
{
    public class DistributionMarketViewModel
    {
        public List<CategoryInfo> AllTopCategories { get; set; }
        /// <summary>
        /// 分享商品基础URI
        /// </summary>
        public string ShareProductUrlTMP { get; set; }
    }
}