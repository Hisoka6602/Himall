﻿using Himall.Entities;
using System;

namespace Himall.DTO
{
    public class OrderRefund
	{
		public long Id { get; set; }
		public long OrderId { get; set; }
		public long OrderItemId { get; set; }
		public long ShopId { get; set; }
		public string ShopName { get; set; }
		public long UserId { get; set; }
		public string Applicant { get; set; }
		public string Reason { get; set; }
		public string ContactPerson { get; set; }
		public string ContactCellPhone { get; set; }
		public string RefundAccount { get; set; }
		public System.DateTime ApplyDate { get; set; }
		public decimal Amount { get; set; }
		public OrderRefundInfo.OrderRefundAuditStatus SellerAuditStatus { get; set; }
		public System.DateTime SellerAuditDate { get; set; }
		public string SellerRemark { get; set; }
		public OrderRefundInfo.OrderRefundConfirmStatus ManagerConfirmStatus { get; set; }
		public System.DateTime ManagerConfirmDate { get; set; }
		public string ManagerRemark { get; set; }
		public bool IsReturn { get; set; }
		public string ExpressCompanyName { get; set; }
		public string ShipOrderNumber { get; set; }
		public string Payee { get; set; }
		public string PayeeAccount { get; set; }
		public OrderRefundInfo.OrderRefundMode RefundMode { get; set; }
		public Nullable<System.DateTime> BuyerDeliverDate { get; set; }
		public Nullable<System.DateTime> SellerConfirmArrivalDate { get; set; }
		public Nullable<OrderRefundInfo.OrderRefundPayStatus> RefundPayStatus { get; set; }
		public Nullable<OrderRefundInfo.OrderRefundPayType> RefundPayType { get; set; }
		public string RefundBatchNo { get; set; }
		public Nullable<System.DateTime> RefundPostTime { get; set; }
		public Nullable<long> ReturnQuantity { get; set; }
		public decimal ReturnBrokerage { get; set; }
		public Nullable<int> ApplyNumber { get; set; }
		public string CertPic1 { get; set; }
		public string CertPic2 { get; set; }
		public string CertPic3 { get; set; }
		public decimal ReturnPlatCommission { get; set; }
		public string ReasonDetail { get; set; }

		/// <summary>
		/// 退货数量
		/// </summary>
		public long ShowReturnQuantity { get; set; }

		/// <summary>
		/// 售后类型
		/// </summary>
		public int RefundType { set; get; }
		/// <summary>
		/// 当前退款状态
		/// </summary>
		public string RefundStatus { get; set; }
		/// <summary>
		/// 当前退款状态
		/// </summary>
		public int? RefundStatusValue { get; set; }
		/// <summary>
		/// 可退金额
		/// <para>订单退款为了(实付+运费-优惠)，单件退款(实付-优惠)</para>
		/// </summary>
		public decimal EnabledRefundAmount { get; set; }

		/// <summary>
		/// 订单是否已超过售后期
		/// </summary>
		public bool IsOrderRefundTimeOut { get; set; }
        /// <summary>
        /// 是否门店订单
        /// <para>手动补值</para>
        /// </summary>
        public bool IsShopBranchOrder { get; set; }
        /// <summary>
        /// 是否虚拟订单
        /// </summary>
        public bool IsVirtual { get; set; }
        /// <summary>
        /// 核销码集合
        /// </summary>
        public string VerificationCodeIds { get; set; }

    }
}