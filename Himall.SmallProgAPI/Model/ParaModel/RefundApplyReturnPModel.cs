﻿using Himall.Entities;

namespace Himall.SmallProgAPI.Model
{
    public class RefundApplyReturnPModel
    {
        public string openId { get; set; }
        public string formId { get; set; }
        public long orderId { get; set; }
        public string skuId { get; set; }
        public long? refundId { get; set; }
        public long? OrderItemId { get; set; }
        public int afterSaleType { get; set; }
        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
        public string ContactPerson { get; set; }
        public string ContactCellPhone { get; set; }
        public OrderRefundInfo.OrderRefundPayType RefundType { get; set; }
        public string RefundReason { get; set; }
        public string Remark { get; set; }
        public string BankName { get; set; }
        public string BankAccountName { get; set; }
        public string BankAccountNo { get; set; }
        public string UserCredentials { get; set; }

        public string ReasonDetail { get; set; }
    }
}
