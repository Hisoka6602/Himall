using Himall.DTO;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    public class DistributorDetailModel
    {
        public Distributor Distributor { get; set; }
        public Distributor SuperiorDistributor { get; set; }

        public DistributionAchievement Achievement { get; set; }
        public int MaxLevel { get; set; }
        public SiteSettings SiteSetting { get; set; }
    }
}