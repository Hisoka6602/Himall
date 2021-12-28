using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Himall.Entities;
using Himall.DTO;

namespace Himall.Web.Areas.Mobile.Models
{
    public class DistributionMyShopViewModel : DistributorInfo
    {
        /// <summary>
        /// 离一下级差多少
        /// </summary>
        public decimal UpgradeNeedAmount { get; set; }
        /// <summary>
        /// 下一级
        /// </summary>
        public string NextGradeName { get; set; }
        /// <summary>
        /// 未结算佣金
        /// </summary>
        public decimal NoSettlementAmount { get; set; }
        public SiteSettings CurrentSiteSettings { get; internal set; }
    }
}