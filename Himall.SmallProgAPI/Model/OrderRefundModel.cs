﻿using Himall.Entities;

namespace Himall.SmallProgAPI.Model
{
    public class OrderRefundModel
    {
        public long MaxRGDNumber { get; set; }

        public decimal MaxRefundAmount { get; set; }

        public OrderItemInfo Item { get; set; }

        public string UserName { get; set; }

        public string Phone { get; set; }

        public OrderInfo OrderInfo { get; set; }

        public long? OrderItemId { get; set; }

        public string RefundWay { get; set; }

        public int BackOut { get; set; }
    }
}
