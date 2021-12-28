﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.API.Model.ParamsModel
{
    public class OrderSubmitFightGroupOrderModel
    {
       public string skuId { get; set; }
        public long count { get; set; }
        public long recieveAddressId { get; set; }
        public long GroupActionId { get; set; }
        public long GroupId { get; set; }

        public bool isCashOnDelivery { get; set; }
        public int invoiceType { get; set; }
        public string invoiceTitle { get; set; }
        public string invoiceContext { get; set; }
        public string invoiceCode { get; set; }
        /// <summary>
        /// 用户APP选择门店自提时用到
        /// </summary>
        public string jsonOrderShops { get; set; }

        public decimal CapitalAmount { get; set; }
        /// <summary>
        /// 支付密码
        /// </summary>
        public string PayPwd { get; set; }
    }
}
