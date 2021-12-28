﻿using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Web.Areas.Mobile.Models
{
    /// <summary>
    /// 礼品订单确认页面模型
    /// </summary>
    public class GiftOrderConfirmPageModel
    {
        /// <summary>
        /// 兑换礼品
        /// </summary>
        public List<GiftOrderItemInfo> GiftList { get; set; }
        /// <summary>
        /// 订单总价
        /// <para>应付积分总数</para>
        /// </summary>
        public int TotalAmount { get; set; }
        /// <summary>
        /// 订单礼品价值总额
        /// </summary>
        public decimal GiftValueTotal { get; set; }
        /// <summary>
        /// 收货地址
        /// </summary>
        public Entities.ShippingAddressInfo ShipAddress { get; set; }
    }
}