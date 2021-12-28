using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Himall.Core.Plugins;
using Himall.DTO;

namespace Himall.Web.Areas.SellerAdmin.Models
{
    public class OrderPrintViewModel
    {
        public int OrdersCount { get; set; }
        public string SenderName { get; set; }
        public string SenderAddress { get; set; }
        public string SenderPhone { get; set; }
        public string SenderRegionId { get; set; }
        public string FullRegionPath { get; set; }
        public IEnumerable<ExpressCompany> Expresses { get; set; }
    }
}