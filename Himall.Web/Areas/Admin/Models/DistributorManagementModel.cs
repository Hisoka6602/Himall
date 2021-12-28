using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Himall.Entities;
using Himall.DTO;

namespace Himall.Web.Areas.Admin.Models
{
    public class DistributorManagementModel
    {
        public List<DistributorGradeInfo> Grades { get; set; }
        public int DistributionMaxLevel { get; set; }
        public SiteSettings SiteSetting { get; set; }

        public long? GradeId { get; set; }
    }
}