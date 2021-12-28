using Himall.DTO;
using Himall.Entities;
using System;
using System.Collections.Generic;

namespace Himall.Web.Areas.Mobile.Models
{
    public class MyGiftsOrderDetailModel
    {
        public GiftOrderInfo OrderData { get; set; }

        public List<GiftOrderItemInfo> OrderItems { get; set; }
        public ExpressData ExpressData { get; set; }
    }
}
